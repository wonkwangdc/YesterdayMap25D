using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Events;
using YesterdayMap.Shelter;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    /// <summary>
    /// Owns only the first-day bunker onboarding flow. Facilities report observations
    /// to this controller and continue to own their normal game rules.
    /// </summary>
    public sealed class DayOneTutorialController : MonoBehaviour
    {
        private const int TutorialDay = 1;
        private static DayOneTutorialController instance;

        private DayCycleManager dayCycle;
        private UIManager ui;
        private PlayerCarryController carryController;
        private GeneratorPowerManager generatorPower;
        private ShelterEventDialogueController eventDialogue;
        private GameObject objectivePanel;
        private Text objectiveText;
        private bool modalOpen;

        private bool introPlayed;
        private bool diaryOpened;
        private bool storageOpened;
        private bool foodConsumed;
        private bool waterConsumed;
        private bool generatorFueled;
        private bool sleepGuideShown;
        private bool completed;

        public static DayOneTutorialController Instance => instance;
        public bool IsWaitingForDiary => IsActive && !diaryOpened;
        public bool IsReadyForSleep =>
            IsActive && diaryOpened && storageOpened &&
            foodConsumed && waterConsumed && generatorFueled &&
            (generatorPower == null || generatorPower.IsRunning);
        private bool IsActive =>
            !completed && dayCycle != null && dayCycle.CurrentDay == TutorialDay;

        public static DayOneTutorialController Ensure(
            GameObject owner,
            DayCycleManager cycle = null,
            UIManager uiManager = null)
        {
            DayOneTutorialController controller =
                FindFirstObjectByType<DayOneTutorialController>();
            if (controller == null && owner != null)
            {
                controller = owner.AddComponent<DayOneTutorialController>();
            }

            controller?.Configure(cycle, uiManager);
            return controller;
        }

        public void Configure(DayCycleManager cycle, UIManager uiManager)
        {
            if (cycle != null) dayCycle = cycle;
            if (uiManager != null) ui = uiManager;
            ResolveReferences();
            BindDayCycle();
            EnsureObjectiveHud();
            RefreshObjective();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            ResolveReferences();
            BindDayCycle();
            EnsureObjectiveHud();

            if (dayCycle != null && dayCycle.CurrentDay > TutorialDay)
            {
                completed = true;
            }

            RefreshObjective();
            if (IsActive && !introPlayed)
            {
                StartCoroutine(PlayArrivalIntroduction());
            }
        }

        private void OnDestroy()
        {
            if (dayCycle != null)
            {
                dayCycle.DayChanged -= HandleDayChanged;
            }

            if (instance == this) instance = null;
        }

        public void NotifyDiaryOpened()
        {
            if (!IsActive) return;
            diaryOpened = true;
            modalOpen = true;
            ui?.ShowMessage(
                "메인 일기에서는 오늘의 기록을, 상태·자원 탭에서는 체력·배고픔·갈증·정신력과 보유 자원을 확인할 수 있습니다.");
            RefreshObjective();
        }

        public void NotifyDiaryClosed()
        {
            modalOpen = false;
            RefreshObjective();
        }

        public void NotifyStorageOpened()
        {
            if (!IsActive) return;
            storageOpened = true;
            modalOpen = true;
            ui?.ShowMessage(
                "창고에서는 보유 물자를 확인합니다. 이번에는 식량과 식수 선반에서 [E]를 눌러 통조림을 먹고 물을 마셔야 합니다.");
            RefreshObjective();
        }

        public void NotifyStorageClosed()
        {
            modalOpen = false;
            RefreshObjective();
        }

        public void NotifyFoodConsumed()
        {
            if (!IsActive) return;
            foodConsumed = true;
            RefreshObjective();
        }

        public void NotifyWaterConsumed()
        {
            if (!IsActive) return;
            waterConsumed = true;
            RefreshObjective();
        }

        public void NotifyFuelCanPickedUp()
        {
            if (!IsActive) return;
            ui?.ShowMessage("연료통을 들었습니다. 발전기 앞에서 [F]를 눌러 연료를 넣으십시오.");
            RefreshObjective();
        }

        public void NotifyGeneratorFueled()
        {
            if (!IsActive) return;
            generatorFueled = true;
            ui?.ShowMessage("발전기 연료 보충을 확인했습니다. 이제 침대에서 하루를 마무리할 수 있습니다.");
            RefreshObjective();
        }

        public void NotifyGeneratorRunningChanged()
        {
            if (!IsActive) return;
            RefreshObjective();
        }

        public bool TryBlockSleep()
        {
            if (!IsActive || IsReadyForSleep) return false;
            if (eventDialogue == null) ResolveReferences();
            eventDialogue?.ShowProtagonistMonologue(CurrentSleepBlockDialogue());
            return true;
        }

        private string CurrentSleepBlockDialogue()
        {
            if (!diaryOpened)
            {
                return "아직 잘 때가 아니다. 먼저 일기장을 펴서 내 상태부터 확인하자.";
            }
            if (!storageOpened)
            {
                return "지금 잠들 수는 없다. 가져온 물건부터 정리하자.";
            }
            if (!foodConsumed && !waterConsumed)
            {
                return "잠들기 전에 몸부터 챙겨야 한다. 통조림을 먹고 물도 마셔 두자.";
            }
            if (!foodConsumed)
            {
                return "아직 몸을 덜 챙겼다. 통조림을 먹고 쉬자.";
            }
            if (!waterConsumed)
            {
                return "아직 몸을 덜 챙겼다. 물을 마시고 쉬자.";
            }
            if (!generatorFueled)
            {
                return "아직 쉴 수 없다. 발전기에 연료부터 채워 두자.";
            }

            return "연료는 채웠다. 발전기를 켜 두고 자자.";
        }

        public bool ConsumeFirstSleepGuide()
        {
            if (!IsReadyForSleep || sleepGuideShown) return false;
            sleepGuideShown = true;
            return true;
        }

        public DayOneTutorialSaveData CaptureState()
        {
            return new DayOneTutorialSaveData
            {
                introPlayed = introPlayed,
                diaryOpened = diaryOpened,
                storageOpened = storageOpened,
                foodConsumed = foodConsumed,
                waterConsumed = waterConsumed,
                generatorFueled = generatorFueled,
                sleepGuideShown = sleepGuideShown,
                completed = completed
            };
        }

        public void RestoreState(DayOneTutorialSaveData data)
        {
            if (data == null) return;
            introPlayed = data.introPlayed;
            diaryOpened = data.diaryOpened;
            storageOpened = data.storageOpened;
            foodConsumed = data.foodConsumed;
            waterConsumed = data.waterConsumed;
            generatorFueled = data.generatorFueled;
            sleepGuideShown = data.sleepGuideShown;
            completed = data.completed;
            RefreshObjective();
        }

        private IEnumerator PlayArrivalIntroduction()
        {
            introPlayed = true;
            yield return new WaitForSecondsRealtime(0.55f);
            if (!IsActive) yield break;
            eventDialogue?.ShowProtagonistMonologue(
                "일단은 들어왔다.",
                ShowSecondArrivalLine,
                "다음");
        }

        private void ShowSecondArrivalLine()
        {
            if (!IsActive) return;
            eventDialogue?.ShowProtagonistMonologue(
                "밖이 어떻게 된 건지는 모르겠지만… 오늘은 여기서 버텨야 한다.");
        }

        private void HandleDayChanged()
        {
            if (dayCycle != null && dayCycle.CurrentDay > TutorialDay)
            {
                completed = true;
            }
            RefreshObjective();
        }

        private void ResolveReferences()
        {
            if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleManager>();
            if (ui == null) ui = FindFirstObjectByType<UIManager>();
            if (carryController == null)
            {
                carryController = FindFirstObjectByType<PlayerCarryController>();
            }
            if (generatorPower == null)
            {
                generatorPower = FindFirstObjectByType<GeneratorPowerManager>();
            }
            if (eventDialogue == null)
            {
                eventDialogue = FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            }
        }

        private void BindDayCycle()
        {
            if (dayCycle == null) return;
            dayCycle.DayChanged -= HandleDayChanged;
            dayCycle.DayChanged += HandleDayChanged;
        }

        private void EnsureObjectiveHud()
        {
            if (objectivePanel != null && objectiveText != null) return;
            Canvas canvas = ui != null ? ui.GetComponent<Canvas>() : null;
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            objectivePanel = new GameObject(
                "DayOneTutorialObjective",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            objectivePanel.transform.SetParent(canvas.transform, false);
            objectivePanel.transform.SetAsLastSibling();

            RectTransform panelRect = objectivePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.85f);
            panelRect.anchorMax = new Vector2(0.85f, 0.975f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = objectivePanel.GetComponent<Image>();
            background.color = new Color(0.035f, 0.03f, 0.02f, 0.86f);
            background.raycastTarget = false;

            GameObject textObject = new(
                "ObjectiveText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(objectivePanel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.035f, 0.08f);
            textRect.anchorMax = new Vector2(0.965f, 0.92f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            objectiveText = textObject.GetComponent<Text>();
            objectiveText.font = ResolveHudFont(canvas);
            objectiveText.fontSize = 20;
            objectiveText.fontStyle = FontStyle.Bold;
            objectiveText.alignment = TextAnchor.MiddleCenter;
            objectiveText.color = new Color(0.94f, 0.88f, 0.68f, 1f);
            objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            objectiveText.verticalOverflow = VerticalWrapMode.Truncate;
            objectiveText.resizeTextForBestFit = false;
            objectiveText.raycastTarget = false;
        }

        private static Font ResolveHudFont(Canvas canvas)
        {
            Text[] texts = canvas.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text != null && text.font != null) return text.font;
            }
            return UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void RefreshObjective()
        {
            if (objectivePanel == null || objectiveText == null) return;
            bool visible = IsActive && !modalOpen;
            objectivePanel.SetActive(visible);
            if (visible)
            {
                objectiveText.text = $"[1일 차 목표]  {CurrentObjectiveText()}";
            }
        }

        private string CurrentObjectiveText()
        {
            if (!diaryOpened)
            {
                return "[Q] 일기장을 열어 상태와 오늘의 기록을 확인하십시오.";
            }
            if (!storageOpened)
            {
                return "창고 앞에서 [F]를 눌러 보유 물자를 확인하십시오.";
            }
            if (!foodConsumed || !waterConsumed)
            {
                string remaining = !foodConsumed && !waterConsumed
                    ? "식량·식수"
                    : !foodConsumed ? "식량" : "식수";
                return $"{remaining} 선반에서 [E]를 눌러 실제로 섭취하십시오.";
            }
            if (!generatorFueled)
            {
                bool carryingFuel = carryController != null && carryController.HasFuelCan;
                return carryingFuel
                    ? "발전기 앞에서 [F]를 눌러 연료통을 넣으십시오."
                    : "연료 선반에서 [E]로 연료통을 들고 발전기에서 [F]로 넣으십시오.";
            }
            if (generatorPower != null && !generatorPower.IsRunning)
            {
                return "발전기 앞에서 [E]를 눌러 발전기를 가동하십시오.";
            }
            return "침대 앞에서 [E]를 눌러 하루를 마무리하십시오.";
        }
    }
}
