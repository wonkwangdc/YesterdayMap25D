using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Resources;
using YesterdayMap.Scavenge;

namespace YesterdayMap.UI
{
    // Displays stored resources and owns the player's normal item-use controls.
    public sealed class StorageUI : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text actionText;
        [SerializeField] private Button closeButton;

        [Header("Use Buttons")]
        [SerializeField] private Button useMedicineButton;
        [SerializeField] private Button useTeddyBearButton;
        [SerializeField] private Button useSoccerBallButton;
        [SerializeField] private Button useGuitarButton;

        [Header("Systems")]
        [SerializeField] private ResourceManager resources;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private UIManager uiManager;

        public void Configure(
            GameObject root,
            Text text,
            Button close,
            ResourceManager manager)
        {
            panel = root;
            resourceText = text;
            closeButton = close;
            resources = manager;
        }

        private void Start()
        {
            ResolveDependencies();
            BindButtons();

            if (resources != null) resources.ResourcesChanged += RefreshIfOpen;
            if (stats != null) stats.StatsChanged += RefreshIfOpen;
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (resources != null) resources.ResourcesChanged -= RefreshIfOpen;
            if (stats != null) stats.StatsChanged -= RefreshIfOpen;

            closeButton?.onClick.RemoveListener(Close);
            useMedicineButton?.onClick.RemoveListener(UseMedicine);
            useTeddyBearButton?.onClick.RemoveListener(UseTeddyBear);
            useSoccerBallButton?.onClick.RemoveListener(UseSoccerBall);
            useGuitarButton?.onClick.RemoveListener(UseGuitar);
        }

        private void Update()
        {
            if (panel != null &&
                panel.activeSelf &&
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open()
        {
            if (panel == null) return;
            ResolveDependencies();
            if (actionText != null) actionText.text = string.Empty;
            Refresh();
            panel.SetActive(true);
            DayOneTutorialController.Instance?.NotifyStorageOpened();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            DayOneTutorialController.Instance?.NotifyStorageClosed();
        }

        public void UseMedicine()
        {
            ResolveDependencies();
            if (resources == null || stats == null)
            {
                ShowAction("구급상자를 사용할 수 없습니다.");
                return;
            }

            if (stats.Health >= 100f)
            {
                ShowAction("체력이 이미 가득 찼습니다.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Medicine, 1))
            {
                ShowAction("보관 중인 구급상자가 없습니다.");
                return;
            }

            stats.ModifyHealth(40f);
            ShowAction("구급상자를 사용했습니다. 체력 +40");
            Refresh();
        }

        public void UseTeddyBear()
        {
            UseMoraleItem(ScavengeItemKind.TeddyBear, 20f);
        }

        public void UseSoccerBall()
        {
            UseMoraleItem(ScavengeItemKind.SoccerBall, 15f);
        }

        public void UseGuitar()
        {
            UseMoraleItem(ScavengeItemKind.Guitar, 25f);
        }

        private void UseMoraleItem(ScavengeItemKind itemKind, float amount)
        {
            ResolveDependencies();
            GameSession session = GameSession.Instance;
            if (session == null || stats == null)
            {
                ShowAction("정신력 회복 물품을 사용할 수 없습니다.");
                return;
            }

            if (stats.Morale >= 100f)
            {
                ShowAction("정신력이 이미 가득 찼습니다.");
                return;
            }

            int currentDay = dayCycle != null ? dayCycle.CurrentDay : 1;
            if (session.GetCollected(itemKind) <= 0)
            {
                ShowAction("아직 수집하지 않은 물품입니다.");
                return;
            }

            if (!session.TryUseMoraleItem(itemKind, currentDay))
            {
                ShowAction("이 물품은 오늘 이미 사용했습니다.");
                return;
            }

            stats.ModifyMorale(amount);
            string itemName = ScavengeManager.KoreanName(
                ResourceType.Parts,
                itemKind);
            ShowAction($"{itemName}을(를) 사용했습니다. 정신력 +{amount:0}");
            Refresh();
        }

        private void RefreshIfOpen()
        {
            if (panel != null && panel.activeSelf) Refresh();
        }

        private void Refresh()
        {
            ResolveDependencies();
            if (resourceText != null && resources != null)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append($"식량 {resources.GetAmount(ResourceType.Food) / 2f:0.0}\n")
                    .Append($"식수 {resources.GetAmount(ResourceType.Water) / 2f:0.0}\n")
                    .Append($"약품 {resources.GetAmount(ResourceType.Medicine)}\n")
                    .Append($"부품 {resources.GetAmount(ResourceType.Parts)}\n")
                    .Append($"배터리 {resources.GetAmount(ResourceType.Battery)}\n")
                    .Append($"연료 {resources.GetAmount(ResourceType.Fuel)}");

                GameSession session = GameSession.Instance;
                if (session != null)
                {
                    AppendStoredItem(builder, session, ScavengeItemKind.KitchenKnife);
                    AppendStoredItem(builder, session, ScavengeItemKind.TeddyBear);
                    AppendStoredItem(builder, session, ScavengeItemKind.Clothes);
                    AppendStoredItem(builder, session, ScavengeItemKind.SoccerBall);
                    AppendStoredItem(builder, session, ScavengeItemKind.BaseballBat);
                    AppendStoredItem(builder, session, ScavengeItemKind.Guitar);
                }

                resourceText.text = builder.ToString();
            }

            RefreshUseButtons();
        }

        private void RefreshUseButtons()
        {
            GameSession session = GameSession.Instance;
            int currentDay = dayCycle != null ? dayCycle.CurrentDay : 1;
            bool canRecoverHealth = stats != null && stats.Health < 100f;
            int medicineCount = resources != null
                ? resources.GetAmount(ResourceType.Medicine)
                : 0;

            ConfigureButton(
                useMedicineButton,
                $"구급상자 사용  x{medicineCount}",
                medicineCount > 0 && canRecoverHealth,
                true);
            ConfigureMoraleButton(
                useTeddyBearButton,
                session,
                ScavengeItemKind.TeddyBear,
                "인형으로 휴식  +20",
                currentDay);
            ConfigureMoraleButton(
                useSoccerBallButton,
                session,
                ScavengeItemKind.SoccerBall,
                "축구공 운동  +15",
                currentDay);
            ConfigureMoraleButton(
                useGuitarButton,
                session,
                ScavengeItemKind.Guitar,
                "기타 연주  +25",
                currentDay);
        }

        private void ConfigureMoraleButton(
            Button button,
            GameSession session,
            ScavengeItemKind itemKind,
            string label,
            int currentDay)
        {
            int count = session != null ? session.GetCollected(itemKind) : 0;
            bool canRecoverMorale = stats != null && stats.Morale < 100f;
            bool canUse = session != null &&
                          session.CanUseMoraleItem(itemKind, currentDay) &&
                          canRecoverMorale;
            bool usedToday = count > 0 && session != null &&
                             !session.CanUseMoraleItem(itemKind, currentDay);
            string suffix = usedToday ? "  (오늘 사용함)" : $"  x{count}";
            ConfigureButton(button, label + suffix, canUse, count > 0);
        }

        private static void ConfigureButton(
            Button button,
            string label,
            bool interactable,
            bool visible)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
            button.interactable = interactable;
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
        }

        private void ShowAction(string message)
        {
            if (actionText != null) actionText.text = message;
            uiManager?.ShowMessage(message);
        }

        private void ResolveDependencies()
        {
            if (resources == null) resources = FindFirstObjectByType<ResourceManager>();
            if (stats == null) stats = FindFirstObjectByType<CharacterStats>();
            if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        }

        private void BindButtons()
        {
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
            useMedicineButton?.onClick.RemoveListener(UseMedicine);
            useMedicineButton?.onClick.AddListener(UseMedicine);
            useTeddyBearButton?.onClick.RemoveListener(UseTeddyBear);
            useTeddyBearButton?.onClick.AddListener(UseTeddyBear);
            useSoccerBallButton?.onClick.RemoveListener(UseSoccerBall);
            useSoccerBallButton?.onClick.AddListener(UseSoccerBall);
            useGuitarButton?.onClick.RemoveListener(UseGuitar);
            useGuitarButton?.onClick.AddListener(UseGuitar);
        }

        private static void AppendStoredItem(
            StringBuilder builder,
            GameSession session,
            ScavengeItemKind itemKind)
        {
            int amount = session.GetCollected(itemKind);
            if (amount <= 0) return;

            builder.Append('\n')
                .Append(ScavengeManager.KoreanName(ResourceType.Parts, itemKind))
                .Append(" x ")
                .Append(amount);
        }
    }
}
