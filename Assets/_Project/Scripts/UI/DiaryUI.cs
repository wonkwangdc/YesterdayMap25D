using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Audio;
using YesterdayMap.CameraSystem;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.Exploration;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.Resources;

namespace YesterdayMap.UI
{
    public sealed class DiaryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button diaryButton;
        [SerializeField] private RawImage diaryIcon;
        [SerializeField] private Outline hoverOutline;
        [SerializeField] private GameObject diaryViewPanel;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button statsTabButton;
        [SerializeField] private Button resourcesTabButton;
        [SerializeField] private Button mainDiaryTabButton;
        [SerializeField] private Button explorationTabButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Text sectionTitle;
        [SerializeField] private Text contentText;
        [SerializeField] private Font diaryContentFont;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private ExplorationManager explorationManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ShelterEventDialogueController eventDialogueController;

        private const int ContentPageLineLimit = 15;
        private const int FinalEndingPageLineLimit = 10;
        private const int ContentPageColumnLimit = 22;
        private const string DiaryControlHint =
            "Q/ESC 닫기 · ↑/↓ 카테고리 · ←/→ 내용 넘기기";
        private const string FinalBunkerDiaryText =
            "오늘은 몸을 움직이는 데 평소보다 시간이 오래 걸렸다.\n" +
            "이제 예전처럼 멀리 나가는 건 어려울 것 같다.\n\n" +
            "라디오는 오늘도 조용했고, 문밖에서도 별다른 소리는 들리지 않았다.\n" +
            "그래도 이곳에서 참 오래 버텼다.\n\n" +
            "누군가 언젠가 이 일기장을 발견한다면,\n" +
            "여기에서 오랫동안 살아간 사람이 있었다는 것 정도는 알 수 있을 것이다.\n\n" +
            "오늘은 조금 일찍 쉬어야겠다.\n" +
            "내일은 몸이 조금 나아졌으면 좋겠다.";
        private const string StarvationEndingDiaryText =
            "오늘도 먹을 것을 찾지 못했다.\n" +
            "빈 통조림을 몇 번이나 다시 확인했지만 달라지는 건 없었다.\n" +
            "몸에 힘이 없어서 일어나는 것조차 쉽지 않다.\n\n" +
            "오늘이 식량을 구할 수 있는 마지막 기회였을지도 모르는데...\n\n" +
            "조금만 더 아꼈다면 달라졌을까.\n" +
            "아니면 마지막 탐사에서 다른 장소를 골랐어야 했을까.\n" +
            "여기까지인 것 같다.";
        private const string DehydrationEndingDiaryText =
            "남아 있던 물은 모두 마셨다.\n" +
            "빈 병을 흔들어봐도 이제는 아무 소리도 나지 않는다.\n" +
            "목이 타는 것보다 머리가 멍해지는 게 더 무섭다.\n\n" +
            "밖에 나가야 했는데, 이제는 일어날 힘도 남지 않았다.\n" +
            "누군가 이 기록을 발견한다면……\n" +
            "물부터 찾아라.";
        private const string LastFrequencyEndingDiaryText =
            "문이 닫힌 첫날에는 이곳에서 며칠이나 버틸 수 있을지만 생각했다.\n" +
            "물과 식량을 세고, 고장 난 시설을 살피고, 문밖의 소리에 귀를 기울였다.\n" +
            "살아남는 일은 거창한 결심보다 오늘 필요한 일을 하나씩 해내는 것이었다.\n\n" +
            "라디오에서 구조 방송이 들리기 시작한 뒤에도 쉽게 믿을 수 없었다.\n" +
            "찾은 기록들은 서로 맞지 않았고, 군 장비를 사용한다는 이유만으로 상대를 믿을 수도 없었다.\n" +
            "그래도 방송에 포함된 이동 정보와 경고는 실제 상황에 맞춰 계속 바뀌고 있었다.\n\n" +
            "외곽 구조 지점에 도착했을 때는 아무도 없었다.\n" +
            "또다시 오지 않는 구조를 기다리게 된 줄 알았다.\n\n" +
            "돌아갈 생각을 하던 순간 라디오에서 새로운 이동 경로가 전달됐다.\n" +
            "잠시 뒤 안개 너머에서 차량 불빛이 나타났다.\n\n" +
            "이 방송은 오래된 녹음이 아니었다.\n" +
            "누군가는 정말로 살아 있는 사람을 찾고 있었다.\n\n" +
            "일기장은 가져간다.\n" +
            "다음 기록을 쓰게 된다면, 그곳은 이 벙커가 아닐 것이다.";
        private const string LastLureBroadcastEndingDiaryText =
            "벙커에 들어온 뒤로 수많은 목소리를 들었다.\n" +
            "도움을 청하는 목소리도 있었고, 살아 있는 사람을 찾는 방송도 있었다.\n" +
            "하지만 어느 쪽이 진실인지 확인할 방법은 없었다.\n\n" +
            "두 번째 주파수는 군 방송보다 가까웠고, 이동 경로도 훨씬 분명했다.\n" +
            "먼 구조 지점을 찾아 헤매는 것보다 확실해 보이는 장소를 선택하는 편이 낫다고 생각했다.\n\n" +
            "가까운 집결 장소에 도착했지만 구조팀은 보이지 않았다.\n" +
            "바닥에는 열린 배낭과 비어 있는 물통이 널려 있었다.\n" +
            "모두 누군가가 급하게 두고 간 것처럼 보인다.\n\n" +
            "방송은 이곳에 설치된 송신기에서 나오고 있었다.\n" +
            "같은 문장만 계속 반복되고 있다.\n\n" +
            "방금 밖에서 여러 사람의 발소리가 들렸다.\n" +
            "한두 명이 아니다.\n\n" +
            "처음부터 이 방송은 구조를 위한 것이 아니라, 사람을 이곳으로—";
        private const string WithStrangersEndingDiaryText =
            "처음 벙커에 들어왔을 때는 혼자라서 다행이라고 생각했다.\n" +
            "물자를 나눌 필요도 없고, 누구의 판단을 의심할 필요도 없었다.\n" +
            "하지만 하루가 지날수록 혼자 할 수 있는 일에는 분명한 한계가 있다는 것을 알게 됐다.\n\n" +
            "문밖에서 만난 사람들도 나와 크게 다르지 않았다.\n" +
            "서로를 완전히 믿지는 못했고, 각자 지키고 싶은 물건과 비밀이 있었다.\n" +
            "그래도 위험한 일을 나누고 부족한 물자를 함께 쓰려는 사람들은 남아 있었다.\n\n" +
            "벙커 밖에서 그들을 다시 만났다.\n" +
            "서로를 믿는 사람은 없었지만, 이번에는 아무도 돌아서지 않았다.\n\n" +
            "가져온 물자를 한곳에 펼쳐 놓고 누가 무엇을 맡을지 정했다.\n" +
            "내 물건이 우리 물자가 되는 일은 아직 낯설다.\n\n" +
            "저녁에는 같은 냄비에서 음식을 나누어 먹었다.\n" +
            "아직 이름을 모르는 사람도 있고, 밤마다 입구를 번갈아 지켜야 한다.\n\n" +
            "불안은 사라지지 않았다.\n" +
            "하지만 내일 아침은 더 이상 혼자 맞지 않는다.";
        private const string RedArmbandEndingDiaryText =
            "혼자 버티는 동안 가장 두려웠던 것은 부족한 물자만이 아니었다.\n" +
            "잠들어 있는 동안 문이 열릴까 걱정했고, 모든 판단의 책임을 혼자 감당해야 했다.\n" +
            "그래서 질서와 보호를 약속하는 사람들의 말이 쉽게 잊히지 않았다.\n\n" +
            "붉은 완장은 분명 위험한 사람들이었다.\n" +
            "그들이 말하는 규칙은 엄격했고, 도움에는 언제나 대가가 따랐다.\n" +
            "그래도 그들의 거점에는 경비와 식량, 살아남기 위한 체계가 있었다.\n\n" +
            "검문소 앞에서 멈추라는 명령을 받았다.\n" +
            "그들은 내 배낭과 물자를 하나씩 확인하고 장부에 적었다.\n\n" +
            "붉은 완장을 건네받았다.\n" +
            "이곳에 머무는 대가는 규칙을 따르는 것이라고 했다.\n\n" +
            "식량은 정해진 시간에 배급되고, 출입구에는 항상 총을 든 경비가 서 있다.\n" +
            "굶거나 밤새 혼자 경계할 일은 없을 것이다.\n\n" +
            "안전한 곳에 들어온 것은 맞다.\n" +
            "다만 이 문을 내 뜻대로 다시 나갈 수 있을지는 모르겠다.";
        private const string DiaryHandsResourcePath =
            "UI/Diary/diary_pull_hand";
        private const float DiaryOpenDuration = 0.54f;
        private const float DiaryCloseDuration = 0.16f;
        private const float DiaryStartScale = 0.72f;
        private const float DiaryCloseScale = 0.96f;
        private const float DiaryStartRotation = 10f;
        private const float DiaryHandsStartOffsetX = 90f;
        private const float DiaryHandsStartOffsetY = -90f;
        private const string UnreadGlowShaderResourcePath =
            "Shaders/DiaryUnreadGlow";
        private const string UnreadGlowShaderName =
            "UI/YesterdayMap/DiaryUnreadGlow";
        private const float UnreadGlowPulseSpeed = 4.2f;
        private const float UnreadGlowMinimumAlpha = 0.48f;
        private const float UnreadGlowMaximumAlpha = 1f;
        private const float UnreadGlowMinimumWidth = 18f;
        private const float UnreadGlowMaximumWidth = 26f;
        private static readonly int GlowColorId =
            Shader.PropertyToID("_GlowColor");
        private static readonly int GlowAlphaId =
            Shader.PropertyToID("_GlowAlpha");
        private static readonly int GlowWidthId =
            Shader.PropertyToID("_GlowWidth");

        private int currentSection;
        private int currentContentPage;
        private readonly List<string> contentPages = new();
        private readonly HashSet<int> unreadMainDiaryPages = new();
        private Button previousContentPageButton;
        private Button nextContentPageButton;
        private Button finalEndingButton;
        private Text contentPageIndicator;
        private RectTransform diaryMotionRoot;
        private CanvasGroup diaryMotionCanvasGroup;
        private RectTransform diaryHandsRect;
        private CanvasGroup diaryHandsCanvasGroup;
        private Image diaryBackdrop;
        private Color diaryBackdropVisibleColor;
        private Coroutine diaryTransition;
        private bool hasBackdropVisibleColor;
        private bool isDiaryOpen;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private bool hasCapturedCursorState;
        private bool isPointerOverDiary;
        private bool hasUnreadResult;
        private bool rebuildUnreadMainDiaryPages;
        private bool finalEndingMode;
        private string finalDeathReason = string.Empty;
        private EndingId finalEndingId = EndingId.None;
        private string pendingUnreadRecordAnchor = string.Empty;
        private bool hasOutlineDefaults;
        private Color defaultOutlineColor;

        private Material defaultDiaryMaterial;
        private Material unreadGlowMaterial;
        private bool hasDefaultDiaryMaterial;
        private bool unreadGlowShaderMissing;
        private Vector2 defaultOutlineDistance;
        private float defaultContentAnchorMinY = 0.14f;
        private bool hasDefaultContentAnchorMinY;

        public bool IsOpen => isDiaryOpen;
        public bool IsFinalEndingConfirmed { get; private set; }

        public void OpenFinalEndingDiary()
        {
            IsFinalEndingConfirmed = false;
            finalEndingMode = true;
            finalDeathReason = string.Empty;
            finalEndingId = EndingId.LastBunker;
            currentSection = 0;
            currentContentPage = 0;
            Open();
            RefreshPage();
        }

        public void OpenLastSurvivalRecordDiary(string deathReason)
        {
            IsFinalEndingConfirmed = false;
            finalEndingMode = true;
            finalDeathReason = deathReason ?? string.Empty;
            finalEndingId = EndingId.LastSurvivalRecord;
            currentSection = 0;
            currentContentPage = 0;
            Open();
            RefreshPage();
        }

        public void OpenSignalEndingDiary(EndingId endingId)
        {
            if (endingId != EndingId.LastFrequency &&
                endingId != EndingId.DoorOpenedNight)
            {
                Debug.LogError(
                    $"지원하지 않는 구조신호 엔딩 일기입니다. Ending: {endingId}",
                    this);
                return;
            }

            IsFinalEndingConfirmed = false;
            finalEndingMode = true;
            finalDeathReason = string.Empty;
            finalEndingId = endingId;
            currentSection = 0;
            currentContentPage = 0;
            Open();
            RefreshPage();
        }

        public void OpenJoinEndingDiary(EndingId endingId)
        {
            if (endingId != EndingId.WithStrangers &&
                endingId != EndingId.RedArmband)
            {
                Debug.LogError(
                    $"지원하지 않는 합류계열 엔딩 일기입니다. Ending: {endingId}",
                    this);
                return;
            }

            IsFinalEndingConfirmed = false;
            finalEndingMode = true;
            finalDeathReason = string.Empty;
            finalEndingId = endingId;
            currentSection = 0;
            currentContentPage = 0;
            Open();
            RefreshPage();
        }

        private void ConfirmFinalEndingDiary()
        {
            if (currentSection != 1 ||
                currentContentPage != contentPages.Count - 1)
            {
                return;
            }

            IsFinalEndingConfirmed = true;
            finalEndingMode = false;
            Close();

            LastBunkerEndingSequence endingSequence =
                FindFirstObjectByType<LastBunkerEndingSequence>(
                    FindObjectsInactive.Include);
            if (endingSequence != null && endingSequence.IsPlaying)
            {
                endingSequence.NotifyFinalDiaryConfirmed();
            }

            LastSurvivalRecordEndingSequence survivalSequence =
                FindFirstObjectByType<LastSurvivalRecordEndingSequence>(
                    FindObjectsInactive.Include);
            if (survivalSequence != null && survivalSequence.IsPlaying)
            {
                survivalSequence.NotifyFinalDiaryConfirmed();
            }

            SignalEndingSequence signalSequence =
                FindFirstObjectByType<SignalEndingSequence>(
                    FindObjectsInactive.Include);
            if (signalSequence != null && signalSequence.IsPlaying)
            {
                signalSequence.NotifyFinalDiaryConfirmed();
            }

            JoinEndingSequence joinSequence =
                FindFirstObjectByType<JoinEndingSequence>(
                    FindObjectsInactive.Include);
            if (joinSequence != null && joinSequence.IsPlaying)
            {
                joinSequence.NotifyFinalDiaryConfirmed();
            }
        }

        public void Configure(
            Button openButton,
            RawImage icon,
            Outline outline,
            GameObject viewPanel,
            Button closeBackdrop)
        {
            diaryButton = openButton;
            diaryIcon = icon;
            hoverOutline = outline;
            diaryViewPanel = viewPanel;
            backdropButton = closeBackdrop;
            EnsureShortcutHintSync();
            ApplyControlHint();
            hasOutlineDefaults = false;
            CaptureOutlineDefaults();
            BindButtons();
            SetHover(false);
            Close();
        }

        public void ConfigureCloseButton(Button button)
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            closeButton = button;
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        public void ConfigureContent(
            Button statsButton,
            Button resourcesButton,
            Button mainButton,
            Button explorationButton,
            Button nextButton,
            Text title,
            Text content,
            CharacterStats characterStats,
            DayCycleManager cycle,
            UIManager manager,
            ExplorationManager exploration,
            ResourceManager resources)
        {
            statsTabButton = statsButton;
            resourcesTabButton = resourcesButton;
            mainDiaryTabButton = mainButton;
            explorationTabButton = explorationButton;
            nextPageButton = nextButton;
            if (nextPageButton != null)
            {
                nextPageButton.gameObject.SetActive(false);
            }
            sectionTitle = title;
            contentText = content;
            stats = characterStats;
            dayCycle = cycle;
            uiManager = manager;
            explorationManager = exploration;
            resourceManager = resources;
            ApplySectionTabLabels();
            EnsureContentPageControls();
            BindSectionButtons();
            RefreshPage();
        }

        public void ConfigureContentFont(Font contentFont)
        {
            diaryContentFont = contentFont;
            ApplyContentFont();
        }

        private void Awake()
        {
            HideLegacyNavigationButtons();
            EnsureShortcutHintSync();
            ApplyControlHint();
            ResolveEventDialogueController();
            EnsureContentPageControls();
            ApplySectionTabLabels();
            BindButtons();
            BindSectionButtons();
            CaptureOutlineDefaults();
            SetHover(false);
            isDiaryOpen = diaryViewPanel != null && diaryViewPanel.activeSelf;
            EnsurePresentationRig();
            if (isDiaryOpen)
            {
                ApplyOpenPose();
            }
            else
            {
                ApplyClosedPose();
            }
        }

        private void Start()
        {
            // KoreanFontApplicator updates the whole Canvas during Awake.
            // Restore the diary paper font afterwards without touching tab buttons.
            ApplyContentFont();
        }

        private void HideLegacyNavigationButtons()
        {
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }

            if (nextPageButton != null)
            {
                nextPageButton.gameObject.SetActive(false);
            }
        }

        private void ApplyContentFont()
        {
            RestoreActionButtonLabels();
            if (diaryContentFont == null)
            {
                return;
            }

            if (sectionTitle != null)
            {
                sectionTitle.font = diaryContentFont;
            }

            if (contentText != null)
            {
                contentText.font = diaryContentFont;
            }

            if (contentPageIndicator != null)
            {
                contentPageIndicator.font = diaryContentFont;
            }
        }

        private void Update()
        {
            UpdateUnreadResultGlow();

            if (diaryViewPanel == null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame ||
                Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (isDiaryOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }

                return;
            }

            if (isDiaryOpen &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (isDiaryOpen &&
                Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                PreviousSection();
                return;
            }

            if (isDiaryOpen &&
                Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                NextPage();
                return;
            }

            if (isDiaryOpen &&
                (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                 Keyboard.current.pageUpKey.wasPressedThisFrame))
            {
                PreviousContentPage();
                return;
            }

            if (isDiaryOpen &&
                (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                 Keyboard.current.pageDownKey.wasPressedThisFrame))
            {
                NextContentPage();
            }
        }

        private void OnDestroy()
        {
            RestoreCursorState();
            ReleaseUnreadGlowMaterial();

            if (diaryTransition != null)
            {
                StopCoroutine(diaryTransition);
            }

            if (diaryButton != null)
            {
                diaryButton.onClick.RemoveListener(Open);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveListener(Close);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            UnbindSectionButtons();
            UnbindContentPageButtons();
        }

        public void Open()
        {
            if (diaryViewPanel == null)
            {
                return;
            }

            PauseMenuUI pauseMenu =
                FindFirstObjectByType<PauseMenuUI>(
                    FindObjectsInactive.Include);
            if (pauseMenu != null && pauseMenu.IsOpen)
            {
                return;
            }

            DayOneTutorialController tutorial = DayOneTutorialController.Instance;
            bool openMainDiaryForTutorial =
                tutorial != null && tutorial.IsWaitingForDiary;

            ShowCursorForDiary();

            bool wasClosed = !isDiaryOpen;
            bool showUnreadResult = hasUnreadResult;
            ResolveEventDialogueController();
            EnsureContentPageControls();
            EnsurePresentationRig();

            if (!diaryViewPanel.activeSelf)
            {
                ApplyClosedPose();
                diaryViewPanel.SetActive(true);
            }

            diaryViewPanel.transform.SetAsLastSibling();
            if (showUnreadResult)
            {
                currentSection = 1;
            }
            else if (openMainDiaryForTutorial)
            {
                currentSection = 1;
            }

            currentContentPage = 0;
            isDiaryOpen = true;
            RefreshPage();
            StartDiaryTransition(true);

            if (wasClosed)
            {
                GameSfxPlayer.Play(GameSfxCue.DiaryOpen);
                tutorial?.NotifyDiaryOpened();
            }
        }

        public void Close()
        {
            if (finalEndingMode && !IsFinalEndingConfirmed)
            {
                return;
            }

            isDiaryOpen = false;
            DayOneTutorialController.Instance?.NotifyDiaryClosed();
            RestoreCursorState();

            if (diaryViewPanel == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                if (diaryTransition != null)
                {
                    StopCoroutine(diaryTransition);
                    diaryTransition = null;
                }

                diaryViewPanel.SetActive(false);
                return;
            }

            if (!diaryViewPanel.activeInHierarchy)
            {
                EnsurePresentationRig();
                ApplyClosedPose();
                diaryViewPanel.SetActive(false);
                return;
            }

            EnsurePresentationRig();
            StartDiaryTransition(false);
        }

        private void ShowCursorForDiary()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!hasCapturedCursorState)
            {
                previousCursorLock = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                hasCapturedCursorState = true;
            }

            IsometricCameraController.SetFirstPersonUiFocus(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreCursorState()
        {
            if (!hasCapturedCursorState)
            {
                return;
            }

            IsometricCameraController.SetFirstPersonUiFocus(false);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            hasCapturedCursorState = false;
        }

        private void OnDisable()
        {
            RestoreCursorState();
        }

        private void EnsurePresentationRig()
        {
            if (diaryViewPanel == null)
            {
                return;
            }

            if (diaryBackdrop == null)
            {
                diaryBackdrop = diaryViewPanel.GetComponent<Image>();
            }

            if (!hasBackdropVisibleColor && diaryBackdrop != null)
            {
                diaryBackdropVisibleColor = diaryBackdrop.color;
                if (diaryBackdropVisibleColor.a <= 0.01f)
                {
                    diaryBackdropVisibleColor.a = 0.86f;
                }

                hasBackdropVisibleColor = true;
            }

            if (diaryMotionRoot == null)
            {
                Transform existingRoot =
                    diaryViewPanel.transform.Find("DiaryMotionRoot");
                if (existingRoot != null)
                {
                    diaryMotionRoot = existingRoot as RectTransform;
                }
                else
                {
                    GameObject rootObject = new(
                        "DiaryMotionRoot",
                        typeof(RectTransform),
                        typeof(CanvasGroup));
                    diaryMotionRoot = rootObject.GetComponent<RectTransform>();
                    diaryMotionRoot.SetParent(diaryViewPanel.transform, false);
                    diaryMotionRoot.anchorMin = Vector2.zero;
                    diaryMotionRoot.anchorMax = Vector2.one;
                    diaryMotionRoot.offsetMin = Vector2.zero;
                    diaryMotionRoot.offsetMax = Vector2.zero;
                    diaryMotionRoot.pivot = new Vector2(0.5f, 0.5f);

                    List<Transform> originalChildren = new();
                    for (int i = 0;
                         i < diaryViewPanel.transform.childCount;
                         i++)
                    {
                        Transform child =
                            diaryViewPanel.transform.GetChild(i);
                        if (child != diaryMotionRoot)
                        {
                            originalChildren.Add(child);
                        }
                    }

                    foreach (Transform child in originalChildren)
                    {
                        child.SetParent(diaryMotionRoot, false);
                    }

                    diaryMotionRoot.SetAsFirstSibling();
                }
            }

            if (diaryMotionRoot == null)
            {
                return;
            }

            diaryMotionCanvasGroup =
                diaryMotionRoot.GetComponent<CanvasGroup>();
            if (diaryMotionCanvasGroup == null)
            {
                diaryMotionCanvasGroup =
                    diaryMotionRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (diaryHandsRect == null)
            {
                Transform existingHands =
                    diaryMotionRoot.Find("DiaryPullHandOverlay");
                GameObject handsObject;
                if (existingHands != null)
                {
                    handsObject = existingHands.gameObject;
                    diaryHandsRect = existingHands as RectTransform;
                }
                else
                {
                    handsObject = new GameObject(
                        "DiaryPullHandOverlay",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(RawImage),
                        typeof(AspectRatioFitter),
                        typeof(CanvasGroup));
                    diaryHandsRect =
                        handsObject.GetComponent<RectTransform>();
                    diaryHandsRect.SetParent(diaryMotionRoot, false);
                    diaryHandsRect.anchorMin = new Vector2(0.5f, 0f);
                    diaryHandsRect.anchorMax = new Vector2(0.5f, 1f);
                    diaryHandsRect.anchoredPosition = Vector2.zero;
                    diaryHandsRect.sizeDelta = Vector2.zero;
                    diaryHandsRect.pivot = new Vector2(0.5f, 0.5f);
                }

                RawImage handsImage = handsObject.GetComponent<RawImage>();
                handsImage.texture =
                    UnityEngine.Resources.Load<Texture2D>(
                        DiaryHandsResourcePath);
                handsImage.color = Color.white;
                handsImage.raycastTarget = false;
                handsObject.SetActive(handsImage.texture != null);

                AspectRatioFitter handsFitter =
                    handsObject.GetComponent<AspectRatioFitter>();
                if (handsFitter == null)
                {
                    handsFitter =
                        handsObject.AddComponent<AspectRatioFitter>();
                }

                handsFitter.aspectMode =
                    AspectRatioFitter.AspectMode.HeightControlsWidth;
                if (handsImage.texture != null &&
                    handsImage.texture.height > 0)
                {
                    handsFitter.aspectRatio =
                        (float)handsImage.texture.width /
                        handsImage.texture.height;
                }

                // Render above the panel backdrop but behind the diary paper.
                diaryHandsRect.SetAsFirstSibling();
            }

            if (diaryHandsRect != null)
            {
                diaryHandsCanvasGroup =
                    diaryHandsRect.GetComponent<CanvasGroup>();
                if (diaryHandsCanvasGroup == null)
                {
                    diaryHandsCanvasGroup =
                        diaryHandsRect.gameObject.AddComponent<CanvasGroup>();
                }

                diaryHandsCanvasGroup.interactable = false;
                diaryHandsCanvasGroup.blocksRaycasts = false;
            }
        }

        private void StartDiaryTransition(bool opening)
        {
            if (!Application.isPlaying)
            {
                if (opening)
                {
                    ApplyOpenPose();
                }
                else
                {
                    ApplyClosedPose();
                    diaryViewPanel.SetActive(false);
                }

                return;
            }

            if (diaryMotionRoot == null ||
                diaryMotionCanvasGroup == null)
            {
                if (!opening)
                {
                    diaryViewPanel.SetActive(false);
                }

                return;
            }

            if (diaryTransition != null)
            {
                StopCoroutine(diaryTransition);
            }

            diaryTransition =
                StartCoroutine(AnimateDiaryTransition(opening));
        }

        private IEnumerator AnimateDiaryTransition(bool opening)
        {
            float duration = opening
                ? DiaryOpenDuration
                : DiaryCloseDuration;
            Vector2 startPosition = diaryMotionRoot.anchoredPosition;
            Vector2 targetPosition = opening
                ? Vector2.zero
                : new Vector2(0f, -40f);
            Vector3 startScale = diaryMotionRoot.localScale;
            Vector3 targetScale = Vector3.one *
                (opening ? 1f : DiaryCloseScale);
            Quaternion startRotation = diaryMotionRoot.localRotation;
            Quaternion targetRotation = opening
                ? Quaternion.identity
                : Quaternion.identity;
            float startRootAlpha = diaryMotionCanvasGroup.alpha;
            float targetRootAlpha = opening ? 1f : 0f;

            if (opening && diaryHandsCanvasGroup != null)
            {
                diaryHandsCanvasGroup.alpha = 1f;
            }

            float startHandsAlpha = diaryHandsCanvasGroup != null
                ? diaryHandsCanvasGroup.alpha
                : 0f;
            float targetHandsAlpha = 0f;
            Vector2 startHandsPosition = diaryHandsRect != null
                ? diaryHandsRect.anchoredPosition
                : Vector2.zero;
            Vector2 targetHandsPosition = opening
                ? Vector2.zero
                : new Vector2(
                    DiaryHandsStartOffsetX,
                    DiaryHandsStartOffsetY);
            float startBackdropAlpha = diaryBackdrop != null
                ? diaryBackdrop.color.a
                : 0f;
            float targetBackdropAlpha = opening && hasBackdropVisibleColor
                ? diaryBackdropVisibleColor.a
                : 0f;

            diaryMotionCanvasGroup.interactable = false;
            diaryMotionCanvasGroup.blocksRaycasts = false;
            if (backdropButton != null)
            {
                backdropButton.interactable = false;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float motionProgress = opening
                    ? EaseOutCubic(normalized)
                    : EaseInCubic(normalized);
                float scaleProgress = opening
                    ? EaseOutBack(normalized)
                    : motionProgress;
                float rootFadeProgress = opening
                    ? Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.08f, 0.72f, normalized))
                    : Mathf.SmoothStep(0f, 1f, normalized);
                float handsFadeProgress = opening
                    ? Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.52f, 0.9f, normalized))
                    : Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0f, 0.35f, normalized));

                diaryMotionRoot.anchoredPosition =
                    Vector2.LerpUnclamped(
                        startPosition,
                        targetPosition,
                        motionProgress);
                diaryMotionRoot.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        targetScale,
                        scaleProgress);
                diaryMotionRoot.localRotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        motionProgress);
                diaryMotionCanvasGroup.alpha =
                    Mathf.Lerp(
                        startRootAlpha,
                        targetRootAlpha,
                        rootFadeProgress);

                if (diaryHandsRect != null)
                {
                    diaryHandsRect.anchoredPosition =
                        Vector2.LerpUnclamped(
                            startHandsPosition,
                            targetHandsPosition,
                            motionProgress);
                }

                if (diaryHandsCanvasGroup != null)
                {
                    diaryHandsCanvasGroup.alpha =
                        Mathf.Lerp(
                            startHandsAlpha,
                            targetHandsAlpha,
                            handsFadeProgress);
                }

                SetBackdropAlpha(
                    Mathf.Lerp(
                        startBackdropAlpha,
                        targetBackdropAlpha,
                        rootFadeProgress));
                yield return null;
            }

            if (opening)
            {
                ApplyOpenPose();
            }
            else
            {
                ApplyClosedPose();
                diaryViewPanel.SetActive(false);
            }

            diaryTransition = null;
        }

        private void ApplyOpenPose()
        {
            if (diaryMotionRoot == null ||
                diaryMotionCanvasGroup == null)
            {
                return;
            }

            diaryMotionRoot.anchoredPosition = Vector2.zero;
            diaryMotionRoot.localScale = Vector3.one;
            diaryMotionRoot.localRotation = Quaternion.identity;
            diaryMotionCanvasGroup.alpha = 1f;
            diaryMotionCanvasGroup.interactable = true;
            diaryMotionCanvasGroup.blocksRaycasts = true;
            if (backdropButton != null)
            {
                backdropButton.interactable = true;
            }

            if (diaryHandsRect != null)
            {
                diaryHandsRect.anchoredPosition = Vector2.zero;
            }

            if (diaryHandsCanvasGroup != null)
            {
                diaryHandsCanvasGroup.alpha = 0f;
            }

            SetBackdropAlpha(hasBackdropVisibleColor
                ? diaryBackdropVisibleColor.a
                : 0.86f);
        }

        private void ApplyClosedPose()
        {
            if (diaryMotionRoot == null ||
                diaryMotionCanvasGroup == null)
            {
                return;
            }

            diaryMotionRoot.anchoredPosition =
                GetHiddenDiaryPosition();
            diaryMotionRoot.localScale =
                Vector3.one * DiaryStartScale;
            diaryMotionRoot.localRotation =
                Quaternion.Euler(0f, 0f, DiaryStartRotation);
            diaryMotionCanvasGroup.alpha = 0f;
            diaryMotionCanvasGroup.interactable = false;
            diaryMotionCanvasGroup.blocksRaycasts = false;
            if (backdropButton != null)
            {
                backdropButton.interactable = false;
            }

            if (diaryHandsRect != null)
            {
                diaryHandsRect.anchoredPosition =
                    new Vector2(
                        DiaryHandsStartOffsetX,
                        DiaryHandsStartOffsetY);
            }

            if (diaryHandsCanvasGroup != null)
            {
                diaryHandsCanvasGroup.alpha = 0f;
            }

            SetBackdropAlpha(0f);
        }

        private Vector2 GetHiddenDiaryPosition()
        {
            RectTransform panelRect =
                diaryViewPanel != null
                    ? diaryViewPanel.transform as RectTransform
                    : null;
            float panelHeight = panelRect != null
                ? panelRect.rect.height
                : 0f;
            float panelWidth = panelRect != null
                ? panelRect.rect.width
                : 0f;
            if (panelHeight < 10f)
            {
                panelHeight = Screen.height;
            }

            if (panelWidth < 10f)
            {
                panelWidth = Screen.width;
            }

            return new Vector2(
                Mathf.Max(180f, panelWidth * 0.3f),
                -Mathf.Max(300f, panelHeight * 0.48f));
        }

        private void SetBackdropAlpha(float alpha)
        {
            if (diaryBackdrop == null)
            {
                return;
            }

            Color color = hasBackdropVisibleColor
                ? diaryBackdropVisibleColor
                : diaryBackdrop.color;
            color.a = alpha;
            diaryBackdrop.color = color;
        }

        private static float EaseInCubic(float value)
        {
            return value * value * value;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            const float coefficient = overshoot + 1f;
            float shifted = value - 1f;
            return 1f + coefficient * shifted * shifted * shifted +
                   overshoot * shifted * shifted;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHover(false);
        }

        public void ShowStats()
        {
            ChangeSection(0);
        }

        public void ShowMainDiary()
        {
            ChangeSection(1);
        }

        public void ShowExplorationHistory()
        {
            ChangeSection(2);
        }

        public void ShowClues()
        {
            ChangeSection(3);
        }

        // Kept for compatibility with older scene bindings.
        public void ShowResources()
        {
            ShowClues();
        }

        public void NextPage()
        {
            ChangeSection((currentSection + 1) % 4);
        }

        public void PreviousSection()
        {
            ChangeSection((currentSection + 3) % 4);
        }

        private void EnsureShortcutHintSync()
        {
            if (diaryIcon == null)
            {
                return;
            }

            Canvas canvas = diaryIcon.canvas;
            if (canvas == null)
            {
                return;
            }

            Graphic[] graphics =
                canvas.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic hintGraphic in graphics)
            {
                if (hintGraphic == null ||
                    hintGraphic.name != "DiaryShortcutHint")
                {
                    continue;
                }

                DiaryShortcutHintSync sync =
                    hintGraphic.GetComponent<DiaryShortcutHintSync>();
                if (sync == null)
                {
                    sync = hintGraphic.gameObject
                        .AddComponent<DiaryShortcutHintSync>();
                }

                sync.Configure(diaryIcon, hintGraphic);
            }
        }

        private void ApplyControlHint()
        {
            if (diaryViewPanel == null)
            {
                return;
            }

            Transform hintTransform =
                diaryViewPanel.transform.Find("DiaryCloseHint");
            if (hintTransform != null &&
                hintTransform.TryGetComponent(out Text hintText))
            {
                hintText.text = DiaryControlHint;
            }
        }

        private void ChangeSection(int section)
        {
            if (currentSection == section && currentContentPage == 0)
            {
                return;
            }

            currentSection = section;
            currentContentPage = 0;
            RefreshPage();

            if (diaryViewPanel != null && diaryViewPanel.activeSelf)
            {
                GameSfxPlayer.Play(GameSfxCue.DiaryPageTurn);
            }
        }

        public void PreviousContentPage()
        {
            if (currentContentPage <= 0)
            {
                return;
            }

            currentContentPage--;
            ApplyContentPage();
        }

        public void NextContentPage()
        {
            if (currentContentPage >= contentPages.Count - 1)
            {
                return;
            }

            currentContentPage++;
            ApplyContentPage();
        }

        private void BindButtons()
        {
            if (diaryButton != null)
            {
                diaryButton.onClick.RemoveListener(Open);
                diaryButton.onClick.AddListener(Open);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveListener(Close);
                backdropButton.onClick.AddListener(Close);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void SetHover(bool highlighted)
        {
            isPointerOverDiary = highlighted;
            RefreshDiaryHighlight();

            if (diaryIcon != null)
            {
                diaryIcon.color = Color.white;
            }
        }

        public static void NotifyUnreadResultAvailable()
        {
            DiaryUI[] diaries = FindObjectsByType<DiaryUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (DiaryUI diary in diaries)
            {
                if (diary != null)
                {
                    diary.MarkUnreadResultAvailable();
                }
            }
        }

        private void MarkUnreadResultAvailable()
        {
            hasUnreadResult = true;
            rebuildUnreadMainDiaryPages = true;
            unreadMainDiaryPages.Clear();
            pendingUnreadRecordAnchor = ResolveLatestUnreadRecordAnchor();
            currentSection = 1;
            currentContentPage = 0;
            RefreshDiaryHighlight();

            if (isDiaryOpen)
            {
                RefreshPage();
            }
        }

        public DiarySaveData CaptureState()
        {
            int[] unreadPages = new int[unreadMainDiaryPages.Count];
            unreadMainDiaryPages.CopyTo(unreadPages);
            Array.Sort(unreadPages);

            return new DiarySaveData
            {
                hasUnreadResult = hasUnreadResult,
                rebuildUnreadMainDiaryPages = rebuildUnreadMainDiaryPages,
                unreadMainDiaryPages = unreadPages
            };
        }

        public void RestoreState(DiarySaveData data)
        {
            unreadMainDiaryPages.Clear();
            hasUnreadResult = data != null && data.hasUnreadResult;
            rebuildUnreadMainDiaryPages = data != null &&
                                            data.rebuildUnreadMainDiaryPages;

            if (data?.unreadMainDiaryPages != null)
            {
                foreach (int page in data.unreadMainDiaryPages)
                {
                    if (page >= 0) unreadMainDiaryPages.Add(page);
                }
            }

            if (!hasUnreadResult)
            {
                rebuildUnreadMainDiaryPages = false;
                unreadMainDiaryPages.Clear();
            }
            else if (!rebuildUnreadMainDiaryPages &&
                     unreadMainDiaryPages.Count == 0)
            {
                // Missing page data from an older save should keep the notice alive.
                rebuildUnreadMainDiaryPages = true;
            }

            if (hasUnreadResult)
            {
                currentSection = 1;
                currentContentPage = 0;
            }

            RefreshDiaryHighlight();
            if (isDiaryOpen) RefreshPage();
        }

        private void ClearUnreadResult()
        {
            hasUnreadResult = false;
            rebuildUnreadMainDiaryPages = false;
            unreadMainDiaryPages.Clear();
            pendingUnreadRecordAnchor = string.Empty;
            RefreshDiaryHighlight();
        }

        private void MarkCurrentContentPageRead()
        {
            if (!isDiaryOpen ||
                currentSection != 1 ||
                rebuildUnreadMainDiaryPages ||
                !hasUnreadResult ||
                !unreadMainDiaryPages.Remove(currentContentPage))
            {
                return;
            }

            if (unreadMainDiaryPages.Count == 0)
            {
                ClearUnreadResult();
                return;
            }

            RefreshDiaryHighlight();
        }

        private void CaptureOutlineDefaults()
        {
            if (hoverOutline == null || hasOutlineDefaults)
            {
                return;
            }

            defaultOutlineColor = hoverOutline.effectColor;
            defaultOutlineDistance = hoverOutline.effectDistance;
            hasOutlineDefaults = true;
        }

        private void RefreshDiaryHighlight()
        {
            if (hoverOutline != null)
            {
                CaptureOutlineDefaults();
                if (hasOutlineDefaults)
                {
                    hoverOutline.effectColor = defaultOutlineColor;
                    hoverOutline.effectDistance = defaultOutlineDistance;
                }

                hoverOutline.enabled = isPointerOverDiary;
            }

            SetUnreadGlowActive(hasUnreadResult);
        }

        private void UpdateUnreadResultGlow()
        {
            if (!hasUnreadResult || !EnsureUnreadGlowMaterial())
            {
                return;
            }

            float pulse = (Mathf.Sin(
                Time.unscaledTime * UnreadGlowPulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(
                UnreadGlowMinimumAlpha,
                UnreadGlowMaximumAlpha,
                pulse);
            float width = Mathf.Lerp(
                UnreadGlowMinimumWidth,
                UnreadGlowMaximumWidth,
                pulse);

            unreadGlowMaterial.SetColor(GlowColorId, Color.white);
            unreadGlowMaterial.SetFloat(GlowAlphaId, alpha);
            unreadGlowMaterial.SetFloat(GlowWidthId, width);
        }

        private void SetUnreadGlowActive(bool active)
        {
            if (diaryIcon == null)
            {
                return;
            }

            if (active)
            {
                if (EnsureUnreadGlowMaterial())
                {
                    diaryIcon.material = unreadGlowMaterial;
                }

                return;
            }

            if (unreadGlowMaterial != null &&
                hasDefaultDiaryMaterial &&
                diaryIcon.material == unreadGlowMaterial)
            {
                diaryIcon.material = defaultDiaryMaterial;
            }
        }

        private bool EnsureUnreadGlowMaterial()
        {
            if (diaryIcon == null || unreadGlowShaderMissing)
            {
                return false;
            }

            if (unreadGlowMaterial != null)
            {
                diaryIcon.material = unreadGlowMaterial;
                return true;
            }

            if (!hasDefaultDiaryMaterial)
            {
                defaultDiaryMaterial = diaryIcon.material;
                hasDefaultDiaryMaterial = true;
            }

            Shader shader = UnityEngine.Resources.Load<Shader>(
                UnreadGlowShaderResourcePath);
            shader ??= Shader.Find(UnreadGlowShaderName);
            if (shader == null)
            {
                unreadGlowShaderMissing = true;
                Debug.LogWarning(
                    $"[{nameof(DiaryUI)}] Unread diary glow shader was not found.");
                return false;
            }

            unreadGlowMaterial = new Material(shader)
            {
                name = "Diary Unread Glow (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            unreadGlowMaterial.SetColor(GlowColorId, Color.white);
            unreadGlowMaterial.SetFloat(
                GlowAlphaId,
                UnreadGlowMinimumAlpha);
            unreadGlowMaterial.SetFloat(
                GlowWidthId,
                UnreadGlowMinimumWidth);
            diaryIcon.material = unreadGlowMaterial;
            return true;
        }

        private void ReleaseUnreadGlowMaterial()
        {
            if (diaryIcon != null &&
                unreadGlowMaterial != null &&
                hasDefaultDiaryMaterial &&
                diaryIcon.material == unreadGlowMaterial)
            {
                diaryIcon.material = defaultDiaryMaterial;
            }

            if (unreadGlowMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(unreadGlowMaterial);
            }
            else
            {
                DestroyImmediate(unreadGlowMaterial);
            }

            unreadGlowMaterial = null;
        }


        private void BindSectionButtons()
        {
            UnbindSectionButtons();
            statsTabButton?.onClick.AddListener(ShowStats);
            resourcesTabButton?.onClick.AddListener(ShowClues);
            mainDiaryTabButton?.onClick.AddListener(ShowMainDiary);
            explorationTabButton?.onClick.AddListener(ShowExplorationHistory);
            nextPageButton?.onClick.AddListener(NextPage);
        }

        private void UnbindSectionButtons()
        {
            statsTabButton?.onClick.RemoveListener(ShowStats);
            resourcesTabButton?.onClick.RemoveListener(ShowResources);
            resourcesTabButton?.onClick.RemoveListener(ShowClues);
            mainDiaryTabButton?.onClick.RemoveListener(ShowMainDiary);
            explorationTabButton?.onClick.RemoveListener(ShowExplorationHistory);
            nextPageButton?.onClick.RemoveListener(NextPage);
        }

        private void RefreshPage()
        {
            if (sectionTitle == null || contentText == null)
                return;

            string fullContent;
            if (finalEndingMode && currentSection == 0)
            {
                sectionTitle.text = GameSession.CurrentPlayerName;
                contentPages.Clear();
                contentPages.Add(BuildFinalStatusPage());
                contentPages.Add(BuildFinalPlayInfoPage());
                currentContentPage = Mathf.Clamp(
                    currentContentPage, 0, contentPages.Count - 1);
                ApplyContentPage();
                SetTabColor(statsTabButton, true);
                SetTabColor(mainDiaryTabButton, false);
                SetTabColor(explorationTabButton, false);
                SetTabColor(resourcesTabButton, false);
                return;
            }

            switch (currentSection)
            {
                case 0:
                    sectionTitle.text = GameSession.CurrentPlayerName;
                    fullContent = BuildStatsText();
                    break;
                case 1:
                    sectionTitle.text =
                        $"{(dayCycle != null ? dayCycle.CurrentDay : 1)}일차";
                    fullContent = BuildMainDiaryText();
                    break;
                case 2:
                    sectionTitle.text = "탐사 기록";
                    fullContent = BuildExplorationText();
                    break;
                default:
                    sectionTitle.text = "판단 단서";
                    fullContent = BuildQuarter3CluesText();
                    break;
            }

            RebuildContentPages(fullContent);
            SetTabColor(statsTabButton, currentSection == 0);
            SetTabColor(mainDiaryTabButton, currentSection == 1);
            SetTabColor(explorationTabButton, currentSection == 2);
            SetTabColor(resourcesTabButton, currentSection == 3);
        }

        private void ApplySectionTabLabels()
        {
            SetButtonLabel(statsTabButton, "상태 · 자원");
            SetButtonLabel(resourcesTabButton, "판단 단서");
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void EnsureContentPageControls()
        {
            if (contentText == null)
            {
                return;
            }

            if (nextPageButton != null)
            {
                Text categoryButtonLabel =
                    nextPageButton.GetComponentInChildren<Text>(true);
                if (categoryButtonLabel != null)
                {
                    categoryButtonLabel.text = "다음 카테고리  ▶";
                }
            }

            Transform page = contentText.transform.parent;
            if (page == null)
            {
                return;
            }

            previousContentPageButton = EnsureContentPageButton(
                page,
                "DiaryPreviousContentPageButton",
                "◀ 내용",
                new Vector2(0.18f, 0.075f),
                new Vector2(0.38f, 0.13f));
            nextContentPageButton = EnsureContentPageButton(
                page,
                "DiaryNextContentPageButton",
                "내용 ▶",
                new Vector2(0.68f, 0.075f),
                new Vector2(0.88f, 0.13f));
            finalEndingButton = EnsureContentPageButton(
                page,
                "DiaryFinalEndingButton",
                "마지막 기록을 덮는다",
                new Vector2(0.34f, 0.16f),
                new Vector2(0.72f, 0.225f));

            Transform indicatorTransform =
                page.Find("DiaryContentPageIndicator");
            if (indicatorTransform == null)
            {
                GameObject indicatorObject = new(
                    "DiaryContentPageIndicator",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                indicatorObject.transform.SetParent(page, false);
                indicatorTransform = indicatorObject.transform;
            }

            RectTransform indicatorRect =
                indicatorTransform.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.40f, 0.075f);
            indicatorRect.anchorMax = new Vector2(0.66f, 0.13f);
            indicatorRect.offsetMin = Vector2.zero;
            indicatorRect.offsetMax = Vector2.zero;

            contentPageIndicator = indicatorTransform.GetComponent<Text>();
            contentPageIndicator.font = contentText.font != null
                ? contentText.font
                : UnityEngine.Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            contentPageIndicator.fontSize = 17;
            contentPageIndicator.alignment = TextAnchor.MiddleCenter;
            contentPageIndicator.color =
                new Color(0.26f, 0.16f, 0.09f, 0.95f);
            contentPageIndicator.raycastTarget = false;
            ApplyContentFont();

            Transform closeHintTransform = null;
            if (diaryViewPanel != null)
            {
                closeHintTransform = diaryMotionRoot != null
                    ? diaryMotionRoot.Find("DiaryCloseHint")
                    : diaryViewPanel.transform.Find("DiaryCloseHint");
            }
            if (closeHintTransform != null &&
                closeHintTransform.TryGetComponent(out Text closeHint))
            {
                closeHint.text =
                    "Q/ESC 닫기 · → 카테고리 · PgUp/PgDn 내용";
            }

            BindContentPageButtons();
        }

        private static Button EnsureContentPageButton(
            Transform parent,
            string objectName,
            string labelText,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Transform existing = parent.Find(objectName);
            GameObject buttonObject;
            if (existing != null)
            {
                buttonObject = existing.gameObject;
            }
            else
            {
                buttonObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.38f, 0.25f, 0.14f, 0.92f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Transform labelTransform = buttonObject.transform.Find("Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                labelTransform = labelObject.transform;
            }

            RectTransform labelRect =
                labelTransform.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelTransform.GetComponent<Text>();
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.94f, 0.88f, 0.76f, 1f);
            label.text = labelText;
            label.raycastTarget = false;
            return button;
        }

        private void BindContentPageButtons()
        {
            UnbindContentPageButtons();
            previousContentPageButton?.onClick.AddListener(
                PreviousContentPage);
            nextContentPageButton?.onClick.AddListener(NextContentPage);
            finalEndingButton?.onClick.AddListener(ConfirmFinalEndingDiary);
        }

        private void UnbindContentPageButtons()
        {
            previousContentPageButton?.onClick.RemoveListener(
                PreviousContentPage);
            nextContentPageButton?.onClick.RemoveListener(NextContentPage);
            finalEndingButton?.onClick.RemoveListener(ConfirmFinalEndingDiary);
        }

        private void RebuildContentPages(string fullContent)
        {
            contentPages.Clear();
            int lineLimit = finalEndingMode && currentSection == 1
                ? FinalEndingPageLineLimit
                : ContentPageLineLimit;
            contentPages.AddRange(BuildContentPages(fullContent, lineLimit));
            if (contentPages.Count == 0)
            {
                contentPages.Add(string.Empty);
            }

            if (currentSection == 1 && rebuildUnreadMainDiaryPages)
            {
                unreadMainDiaryPages.Clear();
                currentContentPage = FindUnreadRecordPage();
                unreadMainDiaryPages.Add(currentContentPage);
                rebuildUnreadMainDiaryPages = false;
                pendingUnreadRecordAnchor = string.Empty;
            }

            currentContentPage = Mathf.Clamp(
                currentContentPage,
                0,
                contentPages.Count - 1);
            ApplyContentPage();
        }

        private int FindUnreadRecordPage()
        {
            string anchor = pendingUnreadRecordAnchor;
            if (string.IsNullOrWhiteSpace(anchor))
            {
                anchor = ResolveLatestUnreadRecordAnchor();
            }

            if (!string.IsNullOrWhiteSpace(anchor))
            {
                for (int i = 0; i < contentPages.Count; i++)
                {
                    if (contentPages[i].IndexOf(
                            anchor,
                            StringComparison.Ordinal) >= 0)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }

        private string ResolveLatestUnreadRecordAnchor()
        {
            ResolveEventDialogueController();
            if (eventDialogueController == null)
            {
                return string.Empty;
            }

            IReadOnlyList<string> eventRecords =
                eventDialogueController.GetEventDiaryRecords();
            if (eventRecords.Count > 0)
            {
                return BuildRecordAnchor(eventRecords[eventRecords.Count - 1]);
            }

            IReadOnlyList<string> todayRecords =
                eventDialogueController.GetTodayDiaryRecords();
            return todayRecords.Count > 0
                ? BuildRecordAnchor(todayRecords[todayRecords.Count - 1])
                : string.Empty;
        }

        private static string BuildRecordAnchor(string record)
        {
            string normalized = (record ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            int lineEnd = normalized.IndexOf('\n');
            return lineEnd >= 0
                ? normalized.Substring(0, lineEnd).Trim()
                : normalized;
        }

        private void ApplyContentPage()
        {
            if (contentText == null || contentPages.Count == 0)
            {
                return;
            }

            contentText.text = contentPages[currentContentPage];
            bool hasMultiplePages = contentPages.Count > 1;
            if (previousContentPageButton != null)
            {
                previousContentPageButton.gameObject.SetActive(
                    hasMultiplePages);
                previousContentPageButton.interactable =
                    currentContentPage > 0;
            }

            if (nextContentPageButton != null)
            {
                nextContentPageButton.gameObject.SetActive(hasMultiplePages);
                nextContentPageButton.interactable =
                    currentContentPage < contentPages.Count - 1;
            }

            if (contentPageIndicator != null)
            {
                contentPageIndicator.gameObject.SetActive(hasMultiplePages);
                contentPageIndicator.text =
                    $"{currentContentPage + 1} / {contentPages.Count}";
            }

            bool showFinalEndingButton =
                finalEndingMode && currentSection == 1 &&
                currentContentPage == contentPages.Count - 1;
            if (finalEndingButton != null)
            {
                finalEndingButton.gameObject.SetActive(showFinalEndingButton);
            }

            RectTransform contentRect = contentText.rectTransform;
            if (!hasDefaultContentAnchorMinY)
            {
                defaultContentAnchorMinY = contentRect.anchorMin.y;
                hasDefaultContentAnchorMinY = true;
            }

            Vector2 contentAnchorMin = contentRect.anchorMin;
            contentAnchorMin.y = showFinalEndingButton
                ? 0.25f
                : defaultContentAnchorMinY;
            contentRect.anchorMin = contentAnchorMin;

            if (showFinalEndingButton)
            {
                finalEndingButton?.transform.SetAsLastSibling();
            }

            MarkCurrentContentPageRead();
        }

        private static IReadOnlyList<string> BuildContentPages(
            string fullContent,
            int lineLimit = ContentPageLineLimit)
        {
            List<string> pages = new();
            string normalized = (fullContent ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
            if (normalized.Length == 0)
            {
                pages.Add(string.Empty);
                return pages;
            }

            string[] blocks = normalized.Split(
                new[] { "\n\n" },
                StringSplitOptions.None);
            StringBuilder page = new();
            int usedLines = 0;
            foreach (string rawBlock in blocks)
            {
                string block = rawBlock.Trim();
                if (block.Length == 0)
                {
                    continue;
                }

                List<string> segments = SplitOversizedBlock(
                    block,
                    lineLimit);
                foreach (string segment in segments)
                {
                    int separatorLines = page.Length > 0 ? 1 : 0;
                    int segmentLines = EstimateWrappedLines(segment);
                    if (page.Length > 0 &&
                        usedLines + separatorLines + segmentLines >
                        lineLimit)
                    {
                        pages.Add(page.ToString());
                        page.Clear();
                        usedLines = 0;
                        separatorLines = 0;
                    }

                    if (page.Length > 0)
                    {
                        page.AppendLine();
                        page.AppendLine();
                    }

                    page.Append(segment);
                    usedLines += separatorLines + segmentLines;
                }
            }

            if (page.Length > 0)
            {
                pages.Add(page.ToString());
            }

            return pages;
        }

        private static List<string> SplitOversizedBlock(
            string block,
            int lineLimit)
        {
            List<string> segments = new();
            string remaining = block;
            while (EstimateWrappedLines(remaining) >
                   lineLimit)
            {
                int low = 1;
                int high = remaining.Length;
                int bestLength = 1;
                while (low <= high)
                {
                    int middle = low + (high - low) / 2;
                    string candidate = remaining.Substring(0, middle);
                    if (EstimateWrappedLines(candidate) <=
                        lineLimit)
                    {
                        bestLength = middle;
                        low = middle + 1;
                    }
                    else
                    {
                        high = middle - 1;
                    }
                }

                int boundary = remaining.LastIndexOfAny(
                    new[] { '.', '!', '?', '\n', ' ' },
                    bestLength - 1,
                    bestLength);
                int minimumUsefulBoundary = Mathf.Max(
                    ContentPageColumnLimit,
                    bestLength - ContentPageColumnLimit * 2);
                int splitLength = boundary >= minimumUsefulBoundary
                    ? boundary + 1
                    : bestLength;
                string segment = remaining
                    .Substring(0, splitLength)
                    .TrimEnd();
                if (segment.Length == 0)
                {
                    splitLength = bestLength;
                    segment = remaining.Substring(0, splitLength);
                }

                segments.Add(segment);
                remaining = remaining.Substring(splitLength).TrimStart();
            }

            if (remaining.Length > 0)
            {
                segments.Add(remaining);
            }

            return segments;
        }

        private static int EstimateWrappedLines(string text)
        {
            string[] lines = text.Split('\n');
            int total = 0;
            foreach (string line in lines)
            {
                total += Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        line.Length /
                        (float)ContentPageColumnLimit));
            }

            return total;
        }

        private string BuildStatsText()
        {
            var builder = new StringBuilder();
            if (stats != null)
            {
                builder.AppendLine($"체력       {stats.Health:0} / 100");
                builder.AppendLine($"허기       {stats.Hunger:0} / 100");
                builder.AppendLine($"갈증       {stats.Thirst:0} / 100");
                builder.AppendLine($"정신력     {stats.Morale:0} / 100");
                builder.AppendLine(
                    $"상태       {(stats.IsCriticalCondition ? "빈사" : "생존")}");
            }
            else
            {
                builder.AppendLine("상태를 확인할 수 없습니다.");
            }

            builder.AppendLine();
            builder.AppendLine("보유 자원");
            builder.AppendLine();
            builder.Append(BuildResourcesText());
            return builder.ToString();
        }

        private string BuildFinalStatusPage()
        {
            var builder = new StringBuilder();
            if (finalEndingId == EndingId.LastFrequency ||
                finalEndingId == EndingId.DoorOpenedNight ||
                finalEndingId == EndingId.WithStrangers ||
                finalEndingId == EndingId.RedArmband)
            {
                if (stats != null)
                {
                    builder.AppendLine($"체력       {stats.Health:0} / 100");
                    builder.AppendLine($"허기       {stats.Hunger:0} / 100");
                    builder.AppendLine($"갈증       {stats.Thirst:0} / 100");
                    builder.AppendLine($"정신력     {stats.Morale:0} / 100");
                }
                else
                {
                    builder.AppendLine("현재 상태를 확인할 수 없습니다.");
                }

                string endingStatus = finalEndingId switch
                {
                    EndingId.LastFrequency => "구조대와 접촉",
                    EndingId.DoorOpenedNight => "연락 두절",
                    EndingId.WithStrangers => "생존자 무리에 합류",
                    EndingId.RedArmband => "붉은 완장에 합류",
                    _ => "기록 종료"
                };
                builder.AppendLine($"상태       {endingStatus}");
                builder.AppendLine();
                builder.AppendLine("마지막 보유 자원");
                builder.AppendLine();
                builder.Append(BuildResourcesText());
                return builder.ToString();
            }

            builder.AppendLine("체력       0 / 100");
            builder.AppendLine("허기       0 / 100");
            builder.AppendLine("갈증       0 / 100");
            builder.AppendLine("정신력     0 / 100");
            string finalStatus = finalDeathReason switch
            {
                "Starvation" => "굶주림으로 인한 사망",
                "Dehydration" => "탈수로 인한 사망",
                _ => "생̶존̶"
            };
            builder.AppendLine($"상태       {finalStatus}");
            builder.AppendLine();
            builder.AppendLine("보유 자원");
            builder.AppendLine();
            builder.Append(BuildFinalResourcesText());
            return builder.ToString();
        }

        private string BuildFinalResourcesText()
        {
            if (resourceManager == null)
                return "보유 자원을 확인할 수 없습니다.";

            float foodDays = resourceManager.GetAmount(ResourceType.Food) / 2f;
            float waterDays = resourceManager.GetAmount(ResourceType.Water) / 2f;
            if (finalDeathReason == "Starvation") foodDays = 0f;
            if (finalDeathReason == "Dehydration") waterDays = 0f;

            return
                $"식량       {foodDays:0.0}일분\n" +
                $"식수       {waterDays:0.0}일분\n" +
                $"약품       {resourceManager.GetAmount(ResourceType.Medicine)}\n" +
                $"수리키트   {resourceManager.GetAmount(ResourceType.Parts)}\n" +
                $"연료       {resourceManager.GetAmount(ResourceType.Fuel)}";
        }

        private string BuildFinalPlayInfoPage()
        {
            GameSession session = GameSession.Instance;
            float totalSeconds = session != null
                ? session.ElapsedPlaySeconds
                : Time.unscaledTime;
            int totalMinutes = Mathf.FloorToInt(totalSeconds / 60f);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            int eventsCompleted = eventDialogueController != null
                ? eventDialogueController.GetEventDiaryRecords().Count
                : 0;
            int explorations = explorationManager != null
                ? explorationManager.ExplorationHistory.Count
                : 0;

            var builder = new StringBuilder();
            builder.AppendLine("플레이 기록");
            builder.AppendLine();
            builder.AppendLine($"플레이 시간       {hours:00}시간 {minutes:00}분");
            builder.AppendLine(
                $"식량을 먹은 횟수   {(session != null ? session.FoodConsumedCount : 0)}회");
            builder.AppendLine(
                $"물을 마신 횟수     {(session != null ? session.WaterConsumedCount : 0)}회");
            builder.AppendLine($"진행한 이벤트     {eventsCompleted}개");
            builder.AppendLine($"탐사한 횟수       {explorations}회");
            builder.AppendLine();
            builder.AppendLine("그가 남긴 기록은 여기까지다.");
            return builder.ToString();
        }

        private string BuildMainDiaryText()
        {
            if (finalEndingMode)
            {
                if (finalEndingId == EndingId.LastFrequency)
                {
                    return "오늘의 일기\n\n" +
                           LastFrequencyEndingDiaryText;
                }

                if (finalEndingId == EndingId.DoorOpenedNight)
                {
                    return "오늘의 일기\n\n" +
                           LastLureBroadcastEndingDiaryText;
                }

                if (finalEndingId == EndingId.WithStrangers)
                {
                    return "오늘의 일기\n\n" +
                           WithStrangersEndingDiaryText;
                }

                if (finalEndingId == EndingId.RedArmband)
                {
                    return "오늘의 일기\n\n" +
                           RedArmbandEndingDiaryText;
                }

                if (!string.IsNullOrEmpty(finalDeathReason))
                {
                    string deathDiaryText = finalDeathReason switch
                    {
                        "Starvation" => StarvationEndingDiaryText,
                        "Dehydration" => DehydrationEndingDiaryText,
                        _ => "마지막 생존 기록의 일기 내용은 남아 있지 않다."
                    };
                    return "오늘의 일기\n\n" + deathDiaryText;
                }

                return "오늘의 일기\n\n" + FinalBunkerDiaryText;
            }

            var builder = new StringBuilder();

            ResolveEventDialogueController();
            IReadOnlyList<string> todayRecords = Array.Empty<string>();
            IReadOnlyList<string> eventRecords = Array.Empty<string>();
            if (eventDialogueController != null)
            {
                eventDialogueController.MarkTodaysJoinStoryViewed();
                todayRecords =
                    eventDialogueController.GetTodayDiaryRecords();
                eventRecords =
                    eventDialogueController.GetEventDiaryRecords();
            }

            builder.AppendLine("오늘의 일기");
            builder.AppendLine();
            if (todayRecords.Count > 0)
            {
                for (int i = todayRecords.Count - 1; i >= 0; i--)
                {
                    builder.AppendLine(todayRecords[i]);
                    if (i > 0)
                    {
                        builder.AppendLine();
                    }
                }
            }
            else
            {
                builder.AppendLine("오늘 기록된 일기가 없습니다.");
            }

            builder.AppendLine();
            builder.AppendLine("──────────");
            builder.AppendLine();
            builder.AppendLine("이벤트 기록");
            builder.AppendLine();
            if (eventRecords.Count > 0)
            {
                for (int i = eventRecords.Count - 1; i >= 0; i--)
                {
                    builder.AppendLine(eventRecords[i]);
                    if (i > 0)
                    {
                        builder.AppendLine();
                        builder.AppendLine("──────────");
                        builder.AppendLine();
                    }
                }
            }
            else
            {
                builder.AppendLine("오늘 기록된 이벤트가 없습니다.");
            }

            if (stats != null && stats.SurvivalRecords.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("생존 경고");
                int start = Mathf.Max(0, stats.SurvivalRecords.Count - 4);
                for (int i = start; i < stats.SurvivalRecords.Count; i++)
                {
                    builder.Append("- ");
                    builder.AppendLine(stats.SurvivalRecords[i]);
                }
            }

            return BuildConditionalDiaryText(todayRecords, eventRecords);
        }

        private string BuildConditionalDiaryText(
            IReadOnlyList<string> todayRecords,
            IReadOnlyList<string> eventRecords)
        {
            bool hasTodayRecords = todayRecords != null &&
                                   todayRecords.Count > 0;
            bool hasEventRecords = eventRecords != null &&
                                   eventRecords.Count > 0;
            bool hasSurvivalRecords = stats != null &&
                                      stats.SurvivalRecords.Count > 0;
            if (!hasTodayRecords && !hasEventRecords &&
                !hasSurvivalRecords && dayCycle != null &&
                dayCycle.CurrentDay == 1)
            {
                return
                    "1일 차\n\n" +
                    "문이 닫히고 나서야 숨을 돌릴 수 있었다.\n" +
                    "위에서 무슨 일이 벌어지고 있는지는 모르겠다.\n" +
                    "우선 가져온 물건부터 정리하고 몸 상태를 확인해야겠다.";
            }

            if (!hasTodayRecords && !hasEventRecords &&
                !hasSurvivalRecords)
            {
                return "기록 내용이 없습니다.";
            }

            var visible = new StringBuilder();
            bool hasPreviousSection = false;
            if (hasTodayRecords)
            {
                visible.AppendLine("오늘의 일기");
                visible.AppendLine();
                AppendRecordsNewestFirst(visible, todayRecords);
                hasPreviousSection = true;
            }

            if (hasEventRecords)
            {
                if (hasPreviousSection)
                {
                    visible.AppendLine();
                    visible.AppendLine();
                }

                AppendRecordsNewestFirst(visible, eventRecords);
                hasPreviousSection = true;
            }

            if (hasSurvivalRecords)
            {
                if (hasPreviousSection)
                {
                    visible.AppendLine();
                    visible.AppendLine();
                }

                visible.AppendLine("생존 경고");
                int start = Mathf.Max(
                    0,
                    stats.SurvivalRecords.Count - 4);
                for (int i = start;
                     i < stats.SurvivalRecords.Count;
                     i++)
                {
                    visible.Append("- ");
                    visible.AppendLine(stats.SurvivalRecords[i]);
                }
            }

            return visible.ToString().TrimEnd();
        }

        private static void AppendRecordsNewestFirst(
            StringBuilder builder,
            IReadOnlyList<string> records)
        {
            for (int i = records.Count - 1; i >= 0; i--)
            {
                builder.Append(records[i]);
                if (i > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }
            }
        }

        private void RestoreActionButtonLabels()
        {
            Font actionFont = DefaultUIFont.Get();
            if (actionFont == null)
            {
                actionFont = diaryContentFont;
            }

            RestoreActionButtonLabel(
                closeButton,
                "닫기  ×",
                actionFont);
            RestoreActionButtonLabel(
                nextPageButton,
                "다음 카테고리  ▶",
                actionFont);
        }

        private static void RestoreActionButtonLabel(
            Button button,
            string label,
            Font font)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null)
            {
                return;
            }

            text.text = label;
            text.enabled = true;
            text.gameObject.SetActive(true);
            if (font != null)
            {
                text.font = font;
            }
        }

        private void ResolveEventDialogueController()
        {
            if (eventDialogueController == null)
            {
                eventDialogueController =
                    FindFirstObjectByType<ShelterEventDialogueController>(
                        FindObjectsInactive.Include);
            }
        }

        private string BuildResourcesText()
        {
            if (resourceManager == null)
                return "보유 자원을 확인할 수 없습니다.";

            return
                $"식량       {resourceManager.GetAmount(ResourceType.Food) / 2f:0.0}일분\n" +
                $"식수       {resourceManager.GetAmount(ResourceType.Water) / 2f:0.0}일분\n" +
                $"약품       {resourceManager.GetAmount(ResourceType.Medicine)}\n" +
                $"수리키트   {resourceManager.GetAmount(ResourceType.Parts)}\n" +
                $"연료       {resourceManager.GetAmount(ResourceType.Fuel)}";
        }

        private string BuildQuarter3CluesText()
        {
            ResolveEventDialogueController();
            if (eventDialogueController == null)
            {
                return "판단 단서를 확인할 수 없습니다.";
            }

            IReadOnlyList<Quarter3ClueDefinition> clues =
                eventDialogueController.GetQuarter3AcquiredClues();
            if (clues.Count == 0)
            {
                return "아직 획득한 판단 단서가 없습니다.";
            }

            var builder = new StringBuilder();
            builder.AppendLine($"획득한 판단 단서 {clues.Count}개");
            builder.AppendLine();
            for (int i = 0; i < clues.Count; i++)
            {
                Quarter3ClueDefinition clue = clues[i];
                builder.AppendLine($"■ 단서{i + 1} · {clue.Title}");
                builder.AppendLine();
                builder.AppendLine(
                    string.IsNullOrWhiteSpace(clue.Content)
                        ? "기록된 내용이 없습니다."
                        : clue.Content);
                if (i < clues.Count - 1)
                {
                    builder.AppendLine();
                    builder.AppendLine("──────────");
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private string BuildExplorationText()
        {
            if (explorationManager == null || explorationManager.ExplorationHistory.Count == 0)
                return "아직 탐사 기록이 없습니다.";

            var builder = new StringBuilder();
            for (int i = explorationManager.ExplorationHistory.Count - 1; i >= 0; i--)
            {
                builder.Append("• ");
                builder.AppendLine(explorationManager.ExplorationHistory[i]);
                if (i > 0)
                    builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void SetTabColor(Button button, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected
                    ? new Color(1f, 0.88f, 0.68f, 1f)
                    : new Color(0.58f, 0.46f, 0.34f, 0.96f);
            }
        }
    }

    internal sealed class DiaryShortcutHintSync : MonoBehaviour
    {
        [SerializeField] private Graphic diaryIcon;
        [SerializeField] private Graphic shortcutHint;
        private PauseMenuUI pauseMenu;

        public void Configure(Graphic icon, Graphic hint)
        {
            diaryIcon = icon;
            shortcutHint = hint;
            ResolvePauseMenu();
            RefreshVisibility();
        }

        private void Awake()
        {
            if (shortcutHint == null)
            {
                shortcutHint = GetComponent<Graphic>();
            }

            ResolvePauseMenu();
        }

        private void OnEnable()
        {
            RefreshVisibility();
        }

        private void LateUpdate()
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (shortcutHint == null)
            {
                return;
            }

            if (pauseMenu == null)
            {
                ResolvePauseMenu();
            }

            bool iconIsVisible =
                diaryIcon != null &&
                diaryIcon.enabled &&
                diaryIcon.gameObject.activeInHierarchy &&
                diaryIcon.color.a > 0.001f &&
                diaryIcon.canvasRenderer.GetInheritedAlpha() > 0.001f;
            bool pauseMenuIsOpen = pauseMenu != null && pauseMenu.IsOpen;
            shortcutHint.enabled = iconIsVisible && !pauseMenuIsOpen;
        }

        private void ResolvePauseMenu()
        {
            pauseMenu = FindFirstObjectByType<PauseMenuUI>(
                FindObjectsInactive.Include);
        }
    }
}
