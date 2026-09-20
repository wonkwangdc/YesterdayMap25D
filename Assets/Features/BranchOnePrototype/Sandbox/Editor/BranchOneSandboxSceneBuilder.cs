using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.BranchOne.Sandbox.Editor
{
    public static class BranchOneSandboxSceneBuilder
    {
        public const string ScenePath =
            "Assets/Features/BranchOnePrototype/Scenes/BranchOneSandbox.unity";

        private static Font legacyFont;

        [MenuItem("Yesterday Map/Branch One/Build Sandbox Scene")]
        public static void BuildScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Save or revert the currently open scene before building BranchOneSandbox.");
            }

            EnsureSceneFolder();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            CreateCamera();
            CreateDirectionalLight();
            CreateEventSystem();

            GameObject canvasObject = CreateCanvas();
            CreateFullScreenImage(
                canvasObject.transform,
                "Background",
                new Color(0.035f, 0.055f, 0.09f, 1f));

            Text statusText;
            Text scoreText;
            CreateHeader(
                canvasObject.transform,
                out statusText,
                out scoreText);

            Button[] eventButtons;
            Text selectedEventTitleText;
            Text selectedEventBodyText;
            Text selectionResultText;
            Button actButton;
            Button declineButton;
            Button advanceButton;
            Button resetButton;
            CreateEventPanel(
                canvasObject.transform,
                out eventButtons,
                out selectedEventTitleText,
                out selectedEventBodyText,
                out selectionResultText,
                out actButton,
                out declineButton,
                out advanceButton,
                out resetButton);

            Text diaryDayText;
            Text mainDiaryText;
            Text eventDiaryText;
            Text explorationDiaryText;
            Button previousDiaryButton;
            Button nextDiaryButton;
            CreateDiaryPanel(
                canvasObject.transform,
                out diaryDayText,
                out mainDiaryText,
                out eventDiaryText,
                out explorationDiaryText,
                out previousDiaryButton,
                out nextDiaryButton);

            GameObject decisionPanel;
            Text decisionDetailsText;
            CreateDecisionPanel(
                canvasObject.transform,
                out decisionPanel,
                out decisionDetailsText);

            BranchSandboxView view = canvasObject.AddComponent<BranchSandboxView>();
            view.Configure(
                statusText,
                scoreText,
                selectedEventTitleText,
                selectedEventBodyText,
                selectionResultText,
                diaryDayText,
                mainDiaryText,
                eventDiaryText,
                explorationDiaryText,
                decisionPanel,
                decisionDetailsText,
                eventButtons,
                actButton,
                declineButton,
                advanceButton,
                resetButton,
                previousDiaryButton,
                nextDiaryButton);

            GameObject presenterObject = new("BranchSandboxPresenter");
            BranchSandboxPresenter presenter =
                presenterObject.AddComponent<BranchSandboxPresenter>();
            presenter.Configure(view, true);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save sandbox scene at {ScenePath}.");
            }

            Selection.activeGameObject = presenterObject;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"BranchOne sandbox scene created: {ScenePath}");
        }

        private static void EnsureSceneFolder()
        {
            const string parent = "Assets/Features/BranchOnePrototype";
            const string scenes = parent + "/Scenes";
            if (!AssetDatabase.IsValidFolder(scenes))
            {
                AssetDatabase.CreateFolder(parent, "Scenes");
            }
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f, 1f);
            camera.orthographic = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.5f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static GameObject CreateCanvas()
        {
            GameObject canvasObject = new(
                "SandboxCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static void CreateHeader(
            Transform parent,
            out Text statusText,
            out Text scoreText)
        {
            GameObject header = CreatePanel(
                parent,
                "HeaderPanel",
                40f,
                20f,
                1840f,
                200f,
                new Color(0.07f, 0.11f, 0.18f, 0.98f));

            CreateText(
                header.transform,
                "TitleText",
                "《어제의 지도》 1분기 Sandbox",
                24f,
                14f,
                1792f,
                42f,
                32,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.94f, 0.97f, 1f));

            statusText = CreateText(
                header.transform,
                "StatusText",
                "현재 상태 준비 중",
                24f,
                68f,
                1040f,
                112f,
                23,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);

            scoreText = CreateText(
                header.transform,
                "ScoreText",
                "점수 준비 중",
                1120f,
                68f,
                690f,
                112f,
                24,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Color(0.55f, 0.82f, 1f));
        }

        private static void CreateEventPanel(
            Transform parent,
            out Button[] eventButtons,
            out Text selectedEventTitleText,
            out Text selectedEventBodyText,
            out Text selectionResultText,
            out Button actButton,
            out Button declineButton,
            out Button advanceButton,
            out Button resetButton)
        {
            GameObject panel = CreatePanel(
                parent,
                "EventPanel",
                40f,
                240f,
                900f,
                800f,
                new Color(0.075f, 0.10f, 0.15f, 0.98f));

            CreateText(
                panel.transform,
                "EventTitleText",
                "오늘의 테스트 이벤트",
                20f,
                16f,
                860f,
                42f,
                28,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white);

            selectedEventTitleText = CreateText(
                panel.transform,
                "SelectedEventTitleText",
                "선택된 이벤트: 아직 선택하지 않았습니다.",
                20f,
                60f,
                860f,
                36f,
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 0.84f, 0.47f));

            selectedEventBodyText = CreateText(
                panel.transform,
                "SelectedEventBodyText",
                "이벤트 버튼을 누르면 제목과 본문이 여기에 표시됩니다.",
                20f,
                100f,
                860f,
                58f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.80f, 0.84f, 0.90f));

            eventButtons = new Button[10];
            for (int index = 0; index < eventButtons.Length; index++)
            {
                int eventNumber = index + 1;
                int column = index % 2;
                int row = index / 2;
                bool signal = eventNumber <= 5;
                string routeText = signal ? "Signal +1" : "Join +1";
                Color color = signal
                    ? new Color(0.10f, 0.38f, 0.68f, 1f)
                    : new Color(0.10f, 0.52f, 0.36f, 1f);
                eventButtons[index] = CreateButton(
                    panel.transform,
                    $"EventButton{eventNumber:00}",
                    $"이벤트 {eventNumber}  |  {routeText}",
                    20f + column * 430f,
                    174f + row * 66f,
                    410f,
                    54f,
                    color);
            }

            actButton = CreateButton(
                panel.transform,
                "ActButton",
                "행동을 한다",
                20f,
                520f,
                410f,
                62f,
                new Color(0.12f, 0.42f, 0.70f, 1f));
            declineButton = CreateButton(
                panel.transform,
                "DeclineButton",
                "행동하지 않는다",
                450f,
                520f,
                430f,
                62f,
                new Color(0.36f, 0.38f, 0.42f, 1f));

            selectionResultText = CreateText(
                panel.transform,
                "SelectionResultText",
                "선택 결과: 아직 선택하지 않았습니다.",
                20f,
                596f,
                860f,
                70f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.95f, 0.84f, 0.47f));

            advanceButton = CreateButton(
                panel.transform,
                "AdvanceDayButton",
                "하루 종료",
                20f,
                690f,
                410f,
                72f,
                new Color(0.67f, 0.34f, 0.08f, 1f));
            resetButton = CreateButton(
                panel.transform,
                "ResetButton",
                "테스트 초기화",
                450f,
                690f,
                430f,
                72f,
                new Color(0.45f, 0.18f, 0.24f, 1f));
        }

        private static void CreateDiaryPanel(
            Transform parent,
            out Text diaryDayText,
            out Text mainDiaryText,
            out Text eventDiaryText,
            out Text explorationDiaryText,
            out Button previousDiaryButton,
            out Button nextDiaryButton)
        {
            GameObject panel = CreatePanel(
                parent,
                "DiaryPanel",
                970f,
                240f,
                910f,
                480f,
                new Color(0.11f, 0.09f, 0.075f, 0.98f));

            diaryDayText = CreateText(
                panel.transform,
                "DiaryDayText",
                "현재 열어본 일기",
                20f,
                16f,
                520f,
                42f,
                27,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.91f, 0.72f));

            previousDiaryButton = CreateButton(
                panel.transform,
                "PreviousDiaryButton",
                "이전 날짜",
                570f,
                14f,
                145f,
                48f,
                new Color(0.32f, 0.27f, 0.20f, 1f));
            nextDiaryButton = CreateButton(
                panel.transform,
                "NextDiaryButton",
                "다음 날짜",
                730f,
                14f,
                160f,
                48f,
                new Color(0.32f, 0.27f, 0.20f, 1f));

            mainDiaryText = CreateSectionText(
                panel.transform,
                "MainDiaryText",
                "메인 일기",
                20f,
                82f,
                870f,
                92f);
            eventDiaryText = CreateSectionText(
                panel.transform,
                "EventDiaryText",
                "이벤트 일기",
                20f,
                184f,
                870f,
                142f);
            explorationDiaryText = CreateSectionText(
                panel.transform,
                "ExplorationDiaryText",
                "탐사 기록",
                20f,
                336f,
                870f,
                124f);
        }

        private static void CreateDecisionPanel(
            Transform parent,
            out GameObject decisionPanel,
            out Text decisionDetailsText)
        {
            decisionPanel = CreatePanel(
                parent,
                "DecisionPanel",
                970f,
                740f,
                910f,
                300f,
                new Color(0.12f, 0.075f, 0.16f, 0.99f));

            CreateText(
                decisionPanel.transform,
                "DecisionTitleText",
                "7일 차 계열 판정 결과",
                20f,
                16f,
                870f,
                44f,
                28,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.90f, 0.72f, 1f));

            decisionDetailsText = CreateText(
                decisionPanel.transform,
                "DecisionDetailsText",
                "판정 대기 중",
                20f,
                70f,
                870f,
                210f,
                21,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
            decisionPanel.SetActive(false);
        }

        private static Text CreateSectionText(
            Transform parent,
            string name,
            string initialText,
            float x,
            float y,
            float width,
            float height)
        {
            GameObject section = CreatePanel(
                parent,
                name + "Background",
                x,
                y,
                width,
                height,
                new Color(0.04f, 0.035f, 0.03f, 0.55f));
            return CreateText(
                section.transform,
                name,
                initialText,
                14f,
                10f,
                width - 28f,
                height - 20f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.96f, 0.93f, 0.86f));
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            SetTopLeftRect(panel.GetComponent<RectTransform>(), x, y, width, height);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static void CreateFullScreenImage(
            Transform parent,
            string name,
            Color color)
        {
            GameObject background = new(name, typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = color;
            background.transform.SetAsFirstSibling();
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            float x,
            float y,
            float width,
            float height,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            SetTopLeftRect(
                textObject.GetComponent<RectTransform>(),
                x,
                y,
                width,
                height);

            Text text = textObject.GetComponent<Text>();
            text.font = GetLegacyFont();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            float x,
            float y,
            float width,
            float height,
            Color normalColor)
        {
            GameObject buttonObject = new(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetTopLeftRect(
                buttonObject.GetComponent<RectTransform>(),
                x,
                y,
                width,
                height);

            Image image = buttonObject.GetComponent<Image>();
            image.color = normalColor;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.16f, 0.17f, 0.19f, 0.72f);
            button.colors = colors;

            Text labelText = CreateText(
                buttonObject.transform,
                "Label",
                label,
                8f,
                6f,
                width - 16f,
                height - 12f,
                21,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 14;
            labelText.resizeTextMaxSize = 21;
            return button;
        }

        private static void SetTopLeftRect(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Font GetLegacyFont()
        {
            if (legacyFont == null)
            {
                legacyFont = AssetDatabase.LoadAssetAtPath<Font>(
                    "Assets/_Project/Scenes/font/SUIT-Regular.otf");
            }

            return legacyFont;
        }
    }
}
