using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Audio;
using YesterdayMap.CameraSystem;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Resources;

using YesterdayMap.Shelter;

namespace YesterdayMap.UI
{
    // Shelter에서 장기 진행과 엔딩을 빠르게 검증하기 위한 관리자 전용 UI입니다.
    public sealed class ShelterAdminDebugUI : MonoBehaviour
    {
        private const int FoodCost = 2;
        private const int WaterCost = 2;
        private const int MedicineCost = 1;
        private const float DebugNeedDecrease = 40f;

        [Header("Data")]
        [SerializeField] private ResourceManager resources;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private GeneratorPowerManager generatorPower;

        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button eatFoodButton;
        [SerializeField] private Button drinkWaterButton;
        [SerializeField] private Button useMedicineButton;
        [SerializeField] private Button restoreMoraleButton;
        [SerializeField] private Button decreaseHealthButton;
        [SerializeField] private Button decreaseMoraleButton;
        [SerializeField] private Button decreaseHungerButton;
        [SerializeField] private Button decreaseThirstButton;
        [SerializeField] private Button addSuppliesButton;
        [SerializeField] private Button fullRestoreButton;
        [SerializeField] private Button generatorOffButton;
        [SerializeField] private Button generatorCriticalButton;
        [SerializeField] private Button generatorFullButton;
        [SerializeField] private Button fuelButton;
        [SerializeField] private Button soccerBallButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private bool startOpen;

        private bool hasAdminAccess;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            hasAdminAccess = IsAuthorizedPlayerName();
            ApplyAdminVisibility();
            if (!hasAdminAccess)
            {
                SetOpen(false);
                return;
            }

            ResolveReferences();
            EnsureSoccerBallButton();
            EnsureFuelButton();
            BindButtons();
            SetOpen(startOpen);
            Refresh();
        }

        private void OnEnable()
        {
            RefreshAdminAccess();
            if (!hasAdminAccess)
            {
                return;
            }

            ResolveReferences();
            EnsureSoccerBallButton();
            EnsureFuelButton();
            BindButtons();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnbindButtons();
            IsometricCameraController.SetFirstPersonUiFocus(false);
        }

        private void Update()
        {
            RefreshAdminAccess();
            if (!hasAdminAccess)
            {
                return;
            }

            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        public void Toggle()
        {
            if (!hasAdminAccess)
            {
                return;
            }

            SetOpen(!IsOpen);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void EatFood()
        {
            if (!CanUseSystems() || stats.Hunger >= 99.9f)
            {
                SetFeedback(stats != null && stats.Hunger >= 99.9f
                    ? "배고픔이 이미 가득합니다."
                    : "관리자 시스템 연결을 확인하세요.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Food, FoodCost))
            {
                SetFeedback("통조림이 부족합니다. 물자 보충을 사용하세요.");
                return;
            }

            stats.ModifyHunger(42f);
            ConsumableUseVisual.Play(ConsumableVisualKind.Food);
            SetFeedback("통조림 1개를 먹어 배고픔을 회복했습니다.");
            GameSfxPlayer.Play(GameSfxCue.UseItem);
        }

        public void DrinkWater()
        {
            if (!CanUseSystems() || stats.Thirst >= 99.9f)
            {
                SetFeedback(stats != null && stats.Thirst >= 99.9f
                    ? "갈증이 이미 가득합니다."
                    : "관리자 시스템 연결을 확인하세요.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Water, WaterCost))
            {
                SetFeedback("물이 부족합니다. 물자 보충을 사용하세요.");
                return;
            }

            stats.ModifyThirst(48f);
            ConsumableUseVisual.Play(ConsumableVisualKind.Water);
            SetFeedback("물 1개를 마셔 갈증을 회복했습니다.");
            GameSfxPlayer.Play(GameSfxCue.DrinkWater);
        }

        public void UseMedicine()
        {
            if (!CanUseSystems() || stats.Health >= 99.9f)
            {
                SetFeedback(stats != null && stats.Health >= 99.9f
                    ? "체력이 이미 가득합니다."
                    : "관리자 시스템 연결을 확인하세요.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Medicine, MedicineCost))
            {
                SetFeedback("구급상자가 부족합니다. 물자 보충을 사용하세요.");
                return;
            }

            stats.ModifyHealth(40f, "Admin medicine");
            SetFeedback("구급상자 1개를 사용해 체력을 회복했습니다.");
        }

        public void RestoreMorale()
        {
            if (stats == null)
            {
                SetFeedback("캐릭터 상태 연결을 확인하세요.");
                return;
            }

            stats.ModifyMorale(30f);
            SetFeedback("관리자 기능으로 정신력을 30 회복했습니다.");
        }

        public void DecreaseHunger()
        {
            if (stats == null)
            {
                SetFeedback("캐릭터 상태 연결을 확인하세요.");
                return;
            }

            stats.ModifyHunger(-DebugNeedDecrease);
            SetFeedback($"테스트용으로 배고픔을 {DebugNeedDecrease:0} 감소시켰습니다.");
        }

        public void DecreaseHealth()
        {
            if (stats == null)
            {
                SetFeedback("캐릭터 상태 연결을 확인하세요.");
                return;
            }

            stats.ModifyHealth(-DebugNeedDecrease, "Admin debug decrease");
            SetFeedback($"테스트용으로 체력을 {DebugNeedDecrease:0} 감소시켰습니다.");
        }

        public void DecreaseMorale()
        {
            if (stats == null)
            {
                SetFeedback("캐릭터 상태 연결을 확인하세요.");
                return;
            }

            stats.ModifyMorale(-DebugNeedDecrease);
            SetFeedback($"테스트용으로 정신력을 {DebugNeedDecrease:0} 감소시켰습니다.");
        }

        public void DecreaseThirst()
        {
            if (stats == null)
            {
                SetFeedback("캐릭터 상태 연결을 확인하세요.");
                return;
            }

            stats.ModifyThirst(-DebugNeedDecrease);
            SetFeedback($"테스트용으로 갈증을 {DebugNeedDecrease:0} 감소시켰습니다.");
        }

        public void AddSupplies()
        {
            if (resources == null)
            {
                SetFeedback("자원 관리자 연결을 확인하세요.");
                return;
            }

            resources.Add(ResourceType.Food, 10);
            resources.Add(ResourceType.Water, 10);
            resources.Add(ResourceType.Medicine, 3);
            SetFeedback("통조림 5개, 물 5개, 구급상자 3개를 보충했습니다.");
        }

        public void AddFuel()
        {
            if (resources == null)
            {
                ResolveReferences();
            }

            if (resources == null)
            {
                SetFeedback("자원 관리자 연결을 확인하세요.");
                return;
            }

            resources.Add(ResourceType.Fuel, 1);
            SetFeedback("연료 1개를 창고에 추가했습니다.");
        }

        public void FullRestore()
        {
            if (stats == null)
            {
                SetFeedback("캐릭터 상태 연결을 확인하세요.");
                return;
            }

            stats.ModifyHealth(100f - stats.Health, "Admin full restore");
            stats.ModifyHunger(100f - stats.Hunger);
            stats.ModifyThirst(100f - stats.Thirst);
            stats.ModifyMorale(100f - stats.Morale);
            SetFeedback("모든 생존 상태를 100으로 회복했습니다.");
        }

        public void SetGeneratorPowerOff()
        {
            SetGeneratorPower(0f, "0% (소등)");
        }

        public void SetGeneratorPowerCritical()
        {
            SetGeneratorPower(20f, "20% (30% 이하 점멸)");
        }

        public void SetGeneratorPowerFull()
        {
            SetGeneratorPower(100f, "100% (정상)");
        }

        public void SpawnOrResetSoccerBall()
        {
            bool spawned = ShelterSoccerBallDebug.SpawnOrReset();
            SetFeedback(spawned
                ? "축구공을 플레이어 앞에 생성했습니다. 다시 누르면 공을 불러옵니다."
                : "축구공 오브젝트를 찾지 못했습니다.");
        }

        private void SetGeneratorPower(float value, string description)
        {
            if (generatorPower == null)
            {
                ResolveReferences();
            }

            if (generatorPower == null)
            {
                SetFeedback("발전기 전력 관리자를 찾지 못했습니다.");
                return;
            }

            generatorPower.SetPowerPercent(value);
            generatorPower.SetRunning(value > 0f);
            SetFeedback($"발전기 전력을 {description} 상태로 설정했습니다.");
        }

        private void HandleGeneratorPowerChanged(float _)
        {
            Refresh();
        }


        private void SetOpen(bool open)
        {
            open = open && hasAdminAccess;
            if (panelRoot != null)
            {
                panelRoot.SetActive(open);
            }

            IsometricCameraController.SetFirstPersonUiFocus(open);
        }

        private bool CanUseSystems() =>
            hasAdminAccess && resources != null && stats != null;

        private static bool IsAuthorizedPlayerName()
        {
            return GameSession.HasAdminAccess;
        }

        private void RefreshAdminAccess()
        {
            bool currentAccess = IsAuthorizedPlayerName();
            if (currentAccess == hasAdminAccess)
            {
                return;
            }

            hasAdminAccess = currentAccess;
            ApplyAdminVisibility();

            if (!hasAdminAccess)
            {
                Unsubscribe();
                UnbindButtons();
                SetOpen(false);
                return;
            }

            ResolveReferences();
            EnsureSoccerBallButton();
            EnsureFuelButton();
            BindButtons();
            Subscribe();
            SetOpen(startOpen);
            Refresh();
        }

        private void ApplyAdminVisibility()
        {
            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(hasAdminAccess);
            }

            if (!hasAdminAccess && panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            if (resources == null)
            {
                resources = FindFirstObjectByType<ResourceManager>(
                    FindObjectsInactive.Include);
            }

            if (stats == null)
            {
                stats = FindFirstObjectByType<CharacterStats>(
                    FindObjectsInactive.Include);
            }

            if (generatorPower == null)
            {
                generatorPower = FindFirstObjectByType<GeneratorPowerManager>(
                    FindObjectsInactive.Include);
            }
        }

        private void Subscribe()
        {
            if (resources != null)
            {
                resources.ResourcesChanged -= Refresh;
                resources.ResourcesChanged += Refresh;
            }

            if (stats != null)
            {
                stats.StatsChanged -= Refresh;
                stats.StatsChanged += Refresh;
            }

            if (generatorPower != null)
            {
                generatorPower.PowerChanged -= HandleGeneratorPowerChanged;
                generatorPower.PowerChanged += HandleGeneratorPowerChanged;
            }
        }

        private void Unsubscribe()
        {
            if (resources != null)
            {
                resources.ResourcesChanged -= Refresh;
            }

            if (stats != null)
            {
                stats.StatsChanged -= Refresh;
            }

            if (generatorPower != null)
            {
                generatorPower.PowerChanged -= HandleGeneratorPowerChanged;
            }
        }

        private void BindButtons()
        {
            Bind(toggleButton, Toggle);
            Bind(closeButton, Close);
            Bind(eatFoodButton, EatFood);
            Bind(drinkWaterButton, DrinkWater);
            Bind(useMedicineButton, UseMedicine);
            Bind(restoreMoraleButton, RestoreMorale);
            Bind(decreaseHealthButton, DecreaseHealth);
            Bind(decreaseMoraleButton, DecreaseMorale);
            Bind(decreaseHungerButton, DecreaseHunger);
            Bind(decreaseThirstButton, DecreaseThirst);
            Bind(addSuppliesButton, AddSupplies);
            Bind(fullRestoreButton, FullRestore);
            Bind(generatorOffButton, SetGeneratorPowerOff);
            Bind(generatorCriticalButton, SetGeneratorPowerCritical);
            Bind(generatorFullButton, SetGeneratorPowerFull);
            Bind(fuelButton, AddFuel);
            Bind(soccerBallButton, SpawnOrResetSoccerBall);
        }

        private void UnbindButtons()
        {
            Unbind(toggleButton, Toggle);
            Unbind(closeButton, Close);
            Unbind(eatFoodButton, EatFood);
            Unbind(drinkWaterButton, DrinkWater);
            Unbind(useMedicineButton, UseMedicine);
            Unbind(restoreMoraleButton, RestoreMorale);
            Unbind(decreaseHealthButton, DecreaseHealth);
            Unbind(decreaseMoraleButton, DecreaseMorale);
            Unbind(decreaseHungerButton, DecreaseHunger);
            Unbind(decreaseThirstButton, DecreaseThirst);
            Unbind(addSuppliesButton, AddSupplies);
            Unbind(fullRestoreButton, FullRestore);
            Unbind(generatorOffButton, SetGeneratorPowerOff);
            Unbind(generatorCriticalButton, SetGeneratorPowerCritical);
            Unbind(generatorFullButton, SetGeneratorPowerFull);
            Unbind(fuelButton, AddFuel);
            Unbind(soccerBallButton, SpawnOrResetSoccerBall);
        }

        private void EnsureFuelButton()
        {
            if (panelRoot == null || fullRestoreButton == null)
            {
                return;
            }

            if (fuelButton == null)
            {
                Transform existing = panelRoot.transform.Find("FuelButton");
                if (existing != null)
                {
                    fuelButton = existing.GetComponent<Button>();
                }
            }

            if (fuelButton == null)
            {
                GameObject buttonObject = Instantiate(
                    fullRestoreButton.gameObject,
                    panelRoot.transform);
                buttonObject.name = "FuelButton";
                fuelButton = buttonObject.GetComponent<Button>();
                fuelButton.onClick.RemoveAllListeners();
            }

            RectTransform buttonRect = fuelButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.04f, 0.02f);
            buttonRect.anchorMax = new Vector2(0.49f, 0.10f);
            buttonRect.offsetMin = new Vector2(4f, 4f);
            buttonRect.offsetMax = new Vector2(-4f, -4f);

            Image buttonImage = fuelButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.42f, 0.27f, 0.10f, 1f);
            }

            Text label = fuelButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "연료 +1";
            }
        }

        private void EnsureSoccerBallButton()
        {
            if (panelRoot == null || fullRestoreButton == null)
            {
                return;
            }

            if (soccerBallButton == null)
            {
                Transform existing = panelRoot.transform.Find("SoccerBallButton");
                if (existing != null)
                {
                    soccerBallButton = existing.GetComponent<Button>();
                }
            }

            if (soccerBallButton == null)
            {
                GameObject buttonObject = Instantiate(
                    fullRestoreButton.gameObject,
                    panelRoot.transform);
                buttonObject.name = "SoccerBallButton";
                soccerBallButton = buttonObject.GetComponent<Button>();
                soccerBallButton.onClick.RemoveAllListeners();

                Image buttonImage = buttonObject.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.16f, 0.35f, 0.18f, 1f);
                }
            }

            RectTransform buttonRect =
                soccerBallButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.51f, 0.02f);
            buttonRect.anchorMax = new Vector2(0.96f, 0.10f);
            buttonRect.offsetMin = new Vector2(4f, 4f);
            buttonRect.offsetMax = new Vector2(-4f, -4f);

            if (statusText != null)
            {
                RectTransform statusRect = statusText.rectTransform;
                statusRect.anchorMin = new Vector2(0.05f, 0.76f);
                statusRect.anchorMax = new Vector2(0.95f, 0.87f);
                statusRect.offsetMin = Vector2.zero;
                statusRect.offsetMax = Vector2.zero;
            }

            if (feedbackText != null)
            {
                RectTransform feedbackRect = feedbackText.rectTransform;
                feedbackRect.anchorMin = new Vector2(0.05f, 0.705f);
                feedbackRect.anchorMax = new Vector2(0.95f, 0.755f);
                feedbackRect.offsetMin = Vector2.zero;
                feedbackRect.offsetMax = Vector2.zero;
                feedbackText.fontSize = 14;
                feedbackText.alignment = TextAnchor.MiddleLeft;
            }

            RefreshSoccerBallButton();
        }

        private void RefreshSoccerBallButton()
        {
            if (soccerBallButton == null)
            {
                return;
            }

            Text label = soccerBallButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "축구공 생성 / 리셋";
            }
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }

            Refresh();
        }

        private void Refresh()
        {
            RefreshSoccerBallButton();

            if (fuelButton != null)
            {
                Text fuelLabel = fuelButton.GetComponentInChildren<Text>(true);
                if (fuelLabel != null)
                {
                    fuelLabel.text = "연료 +1";
                }
            }

            if (statusText != null)
            {
                string generatorStatus = generatorPower == null
                    ? "발전기 연결 없음"
                    : $"발전기 {generatorPower.PowerPercent:0}%  " +
                      GetGeneratorStateLabel(generatorPower.CurrentState);

                if (!CanUseSystems())
                {
                    statusText.text =
                        "관리자 시스템 연결 대기\n" + generatorStatus;
                }
                else
                {
                    statusText.text =
                        $"체력 {stats.Health:0}  배고픔 {stats.Hunger:0}\n" +
                        $"갈증 {stats.Thirst:0}  정신력 {stats.Morale:0}\n" +
                        $"식량 {resources.GetAmount(ResourceType.Food) / 2f:0.#}개  " +
                        $"물 {resources.GetAmount(ResourceType.Water) / 2f:0.#}개  " +
                        $"구급상자 {resources.GetAmount(ResourceType.Medicine)}개  " +
                        $"연료 {resources.GetAmount(ResourceType.Fuel)}개\n" +
                        generatorStatus;
                }
            }

            bool generatorAvailable = generatorPower != null;
            if (generatorOffButton != null)
                generatorOffButton.interactable = generatorAvailable;
            if (generatorCriticalButton != null)
                generatorCriticalButton.interactable = generatorAvailable;
            if (generatorFullButton != null)
                generatorFullButton.interactable = generatorAvailable;

            if (!CanUseSystems()) return;
            if (eatFoodButton != null)
                eatFoodButton.interactable =
                    stats.Hunger < 99.9f &&
                    resources.Has(ResourceType.Food, FoodCost);
            if (drinkWaterButton != null)
                drinkWaterButton.interactable =
                    stats.Thirst < 99.9f &&
                    resources.Has(ResourceType.Water, WaterCost);
            if (useMedicineButton != null)
                useMedicineButton.interactable =
                    stats.Health < 99.9f &&
                    resources.Has(ResourceType.Medicine, MedicineCost);
            if (restoreMoraleButton != null)
                restoreMoraleButton.interactable = stats.Morale < 99.9f;
            if (decreaseHealthButton != null)
                decreaseHealthButton.interactable = stats.Health > 0.1f;
            if (decreaseMoraleButton != null)
                decreaseMoraleButton.interactable = stats.Morale > 0.1f;
            if (decreaseHungerButton != null)
                decreaseHungerButton.interactable = stats.Hunger > 0.1f;
            if (decreaseThirstButton != null)
                decreaseThirstButton.interactable = stats.Thirst > 0.1f;
        }

        private static string GetGeneratorStateLabel(
            GeneratorPowerManager.PowerState state)
        {
            switch (state)
            {
                case GeneratorPowerManager.PowerState.Off:
                    return "(꺼짐)";
                case GeneratorPowerManager.PowerState.Critical:
                    return "(점멸)";
                default:
                    return "(정상)";
            }
        }

    }
}
