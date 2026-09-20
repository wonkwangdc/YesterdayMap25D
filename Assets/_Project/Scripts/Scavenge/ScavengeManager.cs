using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Core;
using YesterdayMap.Resources;
using YesterdayMap.UI;

namespace YesterdayMap.Scavenge
{
    public enum ScavengeItemKind
    {
        Resource,
        KitchenKnife,
        TeddyBear,
        Clothes,
        SoccerBall,
        BaseballBat,
        Guitar
    }

    public sealed class ScavengeManager : MonoBehaviour
    {
        [SerializeField, Min(10f)] private float timeLimit = 60f;
        [SerializeField, Min(1)] private int maximumItems = 4;
        [SerializeField] private Text timerText;
        [SerializeField] private Text messageText;
        [SerializeField] private RadialTimerUI radialTimer;
        [SerializeField] private SceneFader sceneFader;
        [SerializeField] private ScavengeStatusView statusView;
        [Header("Time Expiry Game Over")]
        [SerializeField, Min(0.1f)] private float darknessFadeDuration = 10f;
        [SerializeField, Min(0f)] private float gameOverRevealDelay = 5f;
        [SerializeField, Min(0.1f)] private float initialLightRadius = 1.5f;
        [SerializeField, Min(0.01f)] private float finalLightRadius = 0.4f;
        [Header("Scavenge Pickup Counts")]
        [SerializeField, Min(1)] private int targetFoodPickups = 5;
        [SerializeField, Min(1)] private int targetWaterPickups = 5;
        private readonly List<ResourceType> carriedItems = new();
        private readonly List<int> carriedAmounts = new();
        private readonly List<ScavengeItemKind> carriedItemKinds = new();
        private readonly Dictionary<string, int> depositedItemAmounts = new();
        private readonly List<string> depositedItemOrder = new();
        private float remainingTime;
        private bool timeExpired;
        private bool gameOverRevealed;
        private Transform playerTransform;
        private Camera worldCamera;

        public event Action CarriedItemsChanged;
        public IReadOnlyList<ResourceType> CarriedItems => carriedItems;
        public IReadOnlyList<ScavengeItemKind> CarriedItemKinds => carriedItemKinds;
        public int MaximumItems => maximumItems;
        public bool CanCollect => !timeExpired && carriedItems.Count < maximumItems;
        public bool HasCarriedItems => carriedItems.Count > 0;
        public string BunkerInteractionPrompt => HasCarriedItems
            ? "[E] 물자를 벙커에 반입"
            : timeExpired
                ? "[E] 벙커로 들어가기"
                : "[E] 반입할 물자 없음";

        public void Configure(Text timer, Text message, RadialTimerUI clock, SceneFader fader)
        { timerText = timer; messageText = message; radialTimer = clock; sceneFader = fader; }

        public void ConfigureStatusView(ScavengeStatusView view)
        {
            statusView = view;
        }

        private void Start()
        {
            EnsureStartingResources();
            remainingTime = timeLimit;
            EnsureResourcePickupCounts();
            EnsureScreenOutline();
            if (statusView == null)
            {
                Debug.LogError(
                    "ScavengeStatusView is not assigned. Run the Scavenge status prefab installer.",
                    this);
            }
            else statusView.Initialize(ReturnToMainMenu);
            SetDarkness(0f);
            RefreshDepositedSummary();
            RefreshUI();
            ShowMessage("벙커 입구에서 [E] 물자 반입 · [F] 벙커 진입");
        }

        private static void EnsureStartingResources()
        {
            GameSession session = GameSession.Instance;
            if (session == null ||
                session.GetCollected(ResourceType.Food) > 0)
            {
                return;
            }

            session.Collect(ResourceType.Food, 1);
        }

        private void Update()
        {
            if (timeExpired)
            {
                if (!gameOverRevealed) UpdateDarknessCenter();
                return;
            }
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            UpdateTimeExpiryDarkness();
            if (remainingTime <= 0f)
            {
                ShowTimeExpiryGameOver();
                ShowMessage("좀비가 들어온다. 벙커로 도망쳐야 해.");
            }
            RefreshUI();
        }

        public bool TryCollect(ResourceType type, int amount)
        {
            return TryCollect(type, amount, ScavengeItemKind.Resource);
        }

        public bool TryCollect(ResourceType type, int amount, ScavengeItemKind itemKind)
        {
            if (!CanCollect)
            {
                ShowMessage(timeExpired
                    ? "더 챙길 시간이 없습니다. 벙커 입구에서 [F] 키로 들어가세요."
                    : "가방이 가득 찼습니다. 벙커 입구에서 [E] 반입 · [F] 벙커 진입");
                return false;
            }

            carriedItems.Add(type);
            carriedAmounts.Add(Mathf.Max(1, amount));
            carriedItemKinds.Add(itemKind);
            CarriedItemsChanged?.Invoke();
            ShowMessage($"{KoreanName(type, itemKind)} 획득");
            RefreshUI();
            return true;
        }

        public void InteractWithBunker()
        {
            if (HasCarriedItems)
            {
                DepositCarriedItems();
                return;
            }

            if (timeExpired)
            {
                EnterShelter();
                return;
            }

            ShowMessage("벙커에 반입할 물자가 없습니다.");
        }

        public bool DepositCarriedItems()
        {
            if (!HasCarriedItems)
            {
                ShowMessage("벙커에 반입할 물자가 없습니다.");
                return false;
            }

            GameSession session = GameSession.Instance;
            if (session == null)
            {
                ShowMessage("벙커 저장고에 연결할 수 없습니다.");
                return false;
            }

            int depositedItemCount = 0;
            for (int i = 0; i < carriedItems.Count; i++)
            {
                int amount = i < carriedAmounts.Count ? carriedAmounts[i] : 1;
                ScavengeItemKind itemKind = i < carriedItemKinds.Count
                    ? carriedItemKinds[i]
                    : ScavengeItemKind.Resource;
                AddToDepositedSummary(KoreanName(carriedItems[i], itemKind), 1);
                session.Collect(carriedItems[i], amount, itemKind);
                depositedItemCount++;
            }

            carriedItems.Clear();
            carriedAmounts.Clear();
            carriedItemKinds.Clear();
            CarriedItemsChanged?.Invoke();
            RefreshDepositedSummary();
            ShowMessage($"물자 {depositedItemCount}개를 벙커 저장고에 보관했습니다.");
            RefreshUI();
            return true;
        }

        public void EnterShelter()
        {
            ShowMessage("벙커로 들어갑니다.");
            if (HouseToBunkerTransition.TryBegin("Shelter")) return;
            if (sceneFader != null) sceneFader.LoadScene("Shelter");
            else Debug.LogError("ScavengeManager requires a SceneFader.", this);
        }

        private void RefreshUI()
        {
            if (timerText != null)
                timerText.text = $"수집 {carriedItems.Count}/{maximumItems}";
            radialTimer?.Refresh(remainingTime, timeLimit);
        }

        private void ShowMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        private void UpdateTimeExpiryDarkness()
        {
            float fadeDuration = Mathf.Min(darknessFadeDuration, timeLimit);
            float progress = Mathf.InverseLerp(fadeDuration, 0f, remainingTime);
            UpdateDarknessCenter();
            SetDarkness(progress);
        }

        private void ShowTimeExpiryGameOver()
        {
            if (timeExpired) return;
            timeExpired = true;
            SetDarkness(1f);
            StartCoroutine(RevealGameOverAfterDelay());
        }

        private IEnumerator RevealGameOverAfterDelay()
        {
            yield return new WaitForSecondsRealtime(gameOverRevealDelay);
            gameOverRevealed = true;
            statusView?.RevealGameOver();
            Time.timeScale = 0f;
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            if (timeExpired) Time.timeScale = 1f;
        }

        private void SetDarkness(float alpha)
        {
            float progress = Mathf.Clamp01(alpha);
            float radius = Mathf.Lerp(initialLightRadius, finalLightRadius, progress);
            statusView?.SetDarkness(progress, radius);
        }

        private void UpdateDarknessCenter()
        {
            if (statusView == null) return;
            if (worldCamera == null) worldCamera = Camera.main;
            if (playerTransform == null)
            {
                GameObject player = GameObject.Find("HanDoyoon");
                if (player != null) playerTransform = player.transform;
            }

            Vector2 center = new(0.5f, 0.5f);
            if (worldCamera != null && playerTransform != null)
            {
                Vector3 viewportPoint = worldCamera.WorldToViewportPoint(playerTransform.position + Vector3.up);
                if (viewportPoint.z > 0f) center = new Vector2(viewportPoint.x, viewportPoint.y);
            }
            statusView.SetDarknessCenter(center);
        }

        private void EnsureResourcePickupCounts()
        {
            EnsurePickupCount(ResourceType.Food, targetFoodPickups);
            EnsurePickupCount(ResourceType.Water, targetWaterPickups);
        }

        private static void EnsureScreenOutline()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (mainCamera.GetComponent<ScavengeScreenOutline>() == null)
            {
                mainCamera.gameObject.AddComponent<ScavengeScreenOutline>();
            }
        }

        private void EnsurePickupCount(ResourceType type, int targetCount)
        {
            List<ScavengeCollectible> pickups = new();
            ScavengeCollectible[] scenePickups =
                FindObjectsByType<ScavengeCollectible>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int pickupCount = 0;
            foreach (ScavengeCollectible pickup in scenePickups)
            {
                if (pickup.gameObject.scene != gameObject.scene) continue;
                if (pickup.ItemKind != ScavengeItemKind.Resource ||
                    pickup.ResourceType != type)
                {
                    continue;
                }

                pickups.Add(pickup);
                pickupCount += pickup.VisualPickupCount;
            }

            if (pickups.Count == 0 || pickupCount >= targetCount) return;

            ScavengeCollectible singleVisualSource = null;
            foreach (ScavengeCollectible pickup in pickups)
            {
                if (pickup.VisualPickupCount == 1)
                {
                    singleVisualSource = pickup;
                    break;
                }
            }

            int sourceIndex = 0;
            Vector3[] offsets =
            {
                new(1.25f, 0f, 0.95f),
                new(-1.2f, 0f, 1.1f),
                new(1.1f, 0f, -1.3f),
                new(-1.35f, 0f, -0.9f),
                new(0.2f, 0f, 1.55f)
            };

            while (pickupCount < targetCount)
            {
                ScavengeCollectible source =
                    singleVisualSource != null
                        ? singleVisualSource
                        : pickups[sourceIndex % pickups.Count];
                int extraIndex = pickupCount;
                GameObject clone = Instantiate(
                    source.gameObject,
                    source.transform.parent);
                clone.name = $"{type}_Extra_{pickupCount + 1}";
                clone.transform.position =
                    source.transform.position +
                    offsets[extraIndex % offsets.Length];
                clone.transform.rotation =
                    source.transform.rotation *
                    Quaternion.Euler(0f, 35f * (extraIndex + 1), 0f);
                clone.SetActive(true);

                ScavengeCollectible clonedPickup =
                    clone.GetComponent<ScavengeCollectible>();
                pickups.Add(clonedPickup);
                pickupCount += Mathf.Max(1, clonedPickup.VisualPickupCount);
                sourceIndex++;
            }
        }

        private void AddToDepositedSummary(string itemName, int amount)
        {
            if (!depositedItemAmounts.ContainsKey(itemName))
            {
                depositedItemAmounts[itemName] = 0;
                depositedItemOrder.Add(itemName);
            }

            depositedItemAmounts[itemName] += Mathf.Max(1, amount);
        }

        private void RefreshDepositedSummary()
        {
            bool hasDepositedItems = depositedItemOrder.Count > 0;
            if (!hasDepositedItems)
            {
                statusView?.SetDepositedSummary(string.Empty, false);
                return;
            }

            StringBuilder builder = new StringBuilder("챙긴 물자");
            foreach (string itemName in depositedItemOrder)
            {
                builder.Append('\n')
                    .Append(itemName)
                    .Append(" x ")
                    .Append(depositedItemAmounts[itemName]);
            }

            statusView?.SetDepositedSummary(builder.ToString(), true);
        }

        public static string KoreanName(ResourceType type, ScavengeItemKind itemKind)
        {
            switch (itemKind)
            {
                case ScavengeItemKind.KitchenKnife:
                    return "식칼";
                case ScavengeItemKind.TeddyBear:
                    return "곰인형";
                case ScavengeItemKind.Clothes:
                    return "옷";
                case ScavengeItemKind.SoccerBall:
                    return "축구공";
                case ScavengeItemKind.BaseballBat:
                    return "야구방망이";
                case ScavengeItemKind.Guitar:
                    return "기타";
            }

            return type switch
            {
                ResourceType.Food => "식량",
                ResourceType.Water => "물",
                ResourceType.Medicine => "구급상자",
                ResourceType.Parts => "부품",
                ResourceType.Battery => "배터리",
                ResourceType.Fuel => "연료",
                _ => type.ToString()
            };
        }
    }
}
