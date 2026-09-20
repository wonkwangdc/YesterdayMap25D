using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.BranchOne.Sandbox.Editor
{
    public static class EndingRouteCampaignSandboxSceneBuilder
    {
        public const string ScenePath =
            "Assets/Features/BranchOnePrototype/Scenes/" +
            "EndingRouteCampaignSandbox.unity";

        private static Font legacyFont;

        [MenuItem("Yesterday Map/Build Ending Route Campaign Sandbox")]
        public static void BuildScene()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Save or discard the currently modified scene before " +
                    "building the campaign sandbox.");
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
                new Color(0.025f, 0.038f, 0.065f, 1f));

            CreateHeader(
                canvasObject.transform,
                out Text headerText);
            CreateQuarter1Panel(
                canvasObject.transform,
                out GameObject quarter1Panel,
                out Text quarter1EventTitleText,
                out Text quarter1EventBodyText,
                out Text quarter1SelectionResultText,
                out Button[] quarter1EventButtons,
                out Button quarter1ActButton,
                out Button quarter1DeclineButton,
                out Button quarter1AdvanceDayButton);
            CreateQuarter1ResultPanel(
                canvasObject.transform,
                out GameObject quarter1ResultPanel,
                out Text quarter1DecisionText,
                out Button startQuarter2Button);
            CreateQuarter2Panel(
                canvasObject.transform,
                out GameObject quarter2Panel,
                out Text quarter2EventText,
                out Text quarter2SelectionResultText,
                out Button quarter2ProgressButton,
                out Button quarter2RejectButton,
                out Button quarter2AdvanceButton);
            CreateQuarter2StatusPanel(
                canvasObject.transform,
                out GameObject quarter2RouteStatusPanel,
                out Text quarter2RouteStatusText);
            CreateQuarter3Panel(
                canvasObject.transform,
                out GameObject quarter3Panel,
                out Text quarter3StatusText,
                out Text quarter3SourceProgressText,
                out Text quarter3ResultText,
                out Button quarter3SourceAButton,
                out Button quarter3SourceBButton,
                out Button quarter3OpenFinalChoiceButton,
                out Button quarter3FinalOptionAButton,
                out Button quarter3FinalOptionBButton);
            CreateCampaignResultPanel(
                canvasObject.transform,
                out GameObject campaignResultPanel,
                out Text campaignResultText,
                out Button startQuarter3Button);
            CreateControlPanel(
                canvasObject.transform,
                out GameObject controlPanel,
                out Toggle fixedRandomRollToggle,
                out InputField fixedRandomRollInput,
                out Text randomRollStatusText,
                out Button resetCampaignButton);

            EndingRouteCampaignSandboxView view =
                canvasObject.AddComponent<EndingRouteCampaignSandboxView>();
            view.Configure(
                quarter1Panel,
                quarter1ResultPanel,
                quarter2Panel,
                quarter2RouteStatusPanel,
                quarter3Panel,
                campaignResultPanel,
                controlPanel,
                headerText,
                quarter1EventTitleText,
                quarter1EventBodyText,
                quarter1SelectionResultText,
                quarter1EventButtons,
                quarter1ActButton,
                quarter1DeclineButton,
                quarter1AdvanceDayButton,
                quarter1DecisionText,
                startQuarter2Button,
                quarter2EventText,
                quarter2SelectionResultText,
                quarter2ProgressButton,
                quarter2RejectButton,
                quarter2AdvanceButton,
                quarter2RouteStatusText,
                quarter3StatusText,
                quarter3SourceProgressText,
                quarter3ResultText,
                quarter3SourceAButton,
                quarter3SourceBButton,
                quarter3OpenFinalChoiceButton,
                quarter3FinalOptionAButton,
                quarter3FinalOptionBButton,
                campaignResultText,
                startQuarter3Button,
                fixedRandomRollToggle,
                fixedRandomRollInput,
                randomRollStatusText,
                resetCampaignButton);

            GameObject presenterObject =
                new("EndingRouteCampaignSandboxPresenter");
            EndingRouteCampaignSandboxPresenter presenter =
                presenterObject.AddComponent<
                    EndingRouteCampaignSandboxPresenter>();
            presenter.Configure(view);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save campaign sandbox scene at {ScenePath}.");
            }

            Selection.activeGameObject = presenterObject;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Ending route campaign sandbox created: {ScenePath}");
        }

        private static void EnsureSceneFolder()
        {
            const string parent =
                "Assets/Features/BranchOnePrototype";
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
            camera.backgroundColor =
                new Color(0.025f, 0.038f, 0.065f, 1f);
            camera.orthographic = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position =
                new Vector3(0f, 0f, -10f);
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation =
                Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            _ = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static GameObject CreateCanvas()
        {
            GameObject canvasObject = new(
                "CampaignSandboxCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static void CreateHeader(
            Transform parent,
            out Text headerText)
        {
            GameObject panel = CreatePanel(
                parent,
                "HeaderPanel",
                40f,
                20f,
                1840f,
                130f,
                new Color(0.07f, 0.11f, 0.18f, 0.98f));
            CreateText(
                panel.transform,
                "TitleText",
                "《어제의 지도》 엔딩 계열 통합 Sandbox",
                20f,
                10f,
                1800f,
                38f,
                28,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.92f, 0.96f, 1f));
            headerText = CreateText(
                panel.transform,
                "CampaignHeaderText",
                "캠페인 상태 준비 중",
                20f,
                52f,
                1800f,
                74f,
                18,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
        }

        private static void CreateQuarter1Panel(
            Transform parent,
            out GameObject panel,
            out Text eventTitleText,
            out Text eventBodyText,
            out Text resultText,
            out Button[] eventButtons,
            out Button actButton,
            out Button declineButton,
            out Button advanceButton)
        {
            panel = CreatePanel(
                parent,
                "Quarter1Panel",
                40f,
                170f,
                900f,
                730f,
                new Color(0.065f, 0.095f, 0.15f, 0.98f));
            CreateText(
                panel.transform,
                "Quarter1TitleText",
                "1분기: 2~6일 이벤트, 7일 계열 판정",
                20f,
                12f,
                860f,
                38f,
                27,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.62f, 0.84f, 1f));
            eventTitleText = CreateText(
                panel.transform,
                "Quarter1EventTitleText",
                "선택된 이벤트: 아직 선택하지 않았습니다.",
                20f,
                54f,
                860f,
                34f,
                21,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.96f, 0.83f, 0.46f));
            eventBodyText = CreateText(
                panel.transform,
                "Quarter1EventBodyText",
                "이벤트를 선택하면 본문이 표시됩니다.",
                20f,
                92f,
                860f,
                54f,
                18,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.82f, 0.86f, 0.92f));

            eventButtons = new Button[10];
            for (int index = 0; index < eventButtons.Length; index++)
            {
                int eventNumber = index + 1;
                int column = index % 2;
                int row = index / 2;
                bool signal = eventNumber <= 5;
                eventButtons[index] = CreateButton(
                    panel.transform,
                    $"CampaignEventButton{eventNumber:00}",
                    $"이벤트 {eventNumber} | " +
                    (signal ? "Signal +1" : "Join +1"),
                    20f + column * 430f,
                    160f + row * 62f,
                    410f,
                    50f,
                    signal
                        ? new Color(0.10f, 0.38f, 0.68f, 1f)
                        : new Color(0.10f, 0.52f, 0.36f, 1f));
            }

            actButton = CreateButton(
                panel.transform,
                "Quarter1ActButton",
                "행동을 한다",
                20f,
                482f,
                410f,
                58f,
                new Color(0.12f, 0.42f, 0.70f, 1f));
            declineButton = CreateButton(
                panel.transform,
                "Quarter1DeclineButton",
                "행동하지 않는다",
                450f,
                482f,
                430f,
                58f,
                new Color(0.36f, 0.38f, 0.42f, 1f));
            resultText = CreateText(
                panel.transform,
                "Quarter1SelectionResultText",
                "선택 결과 대기 중",
                20f,
                552f,
                860f,
                82f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.96f, 0.84f, 0.48f));
            advanceButton = CreateButton(
                panel.transform,
                "Quarter1AdvanceDayButton",
                "하루 종료",
                20f,
                646f,
                860f,
                62f,
                new Color(0.66f, 0.34f, 0.08f, 1f));
        }

        private static void CreateQuarter1ResultPanel(
            Transform parent,
            out GameObject panel,
            out Text decisionText,
            out Button startQuarter2Button)
        {
            panel = CreatePanel(
                parent,
                "Quarter1ResultPanel",
                960f,
                170f,
                920f,
                450f,
                new Color(0.11f, 0.07f, 0.16f, 0.99f));
            CreateText(
                panel.transform,
                "Quarter1ResultTitleText",
                "1분기 판정 결과",
                22f,
                16f,
                876f,
                42f,
                29,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.90f, 0.72f, 1f));
            decisionText = CreateText(
                panel.transform,
                "Quarter1DecisionText",
                "판정 대기 중",
                22f,
                70f,
                876f,
                260f,
                22,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
            startQuarter2Button = CreateButton(
                panel.transform,
                "StartQuarter2Button",
                "2분기 시작",
                22f,
                352f,
                876f,
                72f,
                new Color(0.47f, 0.22f, 0.66f, 1f));
            panel.SetActive(false);
        }

        private static void CreateQuarter2Panel(
            Transform parent,
            out GameObject panel,
            out Text eventText,
            out Text resultText,
            out Button progressButton,
            out Button rejectButton,
            out Button advanceButton)
        {
            panel = CreatePanel(
                parent,
                "Quarter2Panel",
                960f,
                170f,
                920f,
                450f,
                new Color(0.075f, 0.12f, 0.12f, 0.99f));
            CreateText(
                panel.transform,
                "Quarter2TitleText",
                "2분기 계열 이벤트",
                22f,
                14f,
                876f,
                40f,
                29,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.64f, 0.96f, 0.78f));
            eventText = CreateText(
                panel.transform,
                "Quarter2EventText",
                "2분기 시작 전입니다.",
                22f,
                62f,
                876f,
                174f,
                21,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
            resultText = CreateText(
                panel.transform,
                "Quarter2SelectionResultText",
                "선택 결과 대기 중",
                22f,
                242f,
                876f,
                62f,
                18,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.96f, 0.84f, 0.48f));
            progressButton = CreateButton(
                panel.transform,
                "Quarter2ProgressButton",
                "진행한다",
                22f,
                316f,
                420f,
                54f,
                new Color(0.10f, 0.52f, 0.36f, 1f));
            rejectButton = CreateButton(
                panel.transform,
                "Quarter2RejectButton",
                "거절한다",
                458f,
                316f,
                440f,
                54f,
                new Color(0.56f, 0.18f, 0.20f, 1f));
            advanceButton = CreateButton(
                panel.transform,
                "Quarter2AdvanceButton",
                "다음 이벤트",
                22f,
                382f,
                876f,
                50f,
                new Color(0.57f, 0.34f, 0.08f, 1f));
            panel.SetActive(false);
        }

        private static void CreateQuarter2StatusPanel(
            Transform parent,
            out GameObject panel,
            out Text routeStatusText)
        {
            panel = CreatePanel(
                parent,
                "Quarter2RouteStatusPanel",
                960f,
                640f,
                920f,
                260f,
                new Color(0.08f, 0.09f, 0.13f, 0.99f));
            CreateText(
                panel.transform,
                "Quarter2RouteStatusTitleText",
                "2분기 계열별 진행 상태",
                22f,
                14f,
                876f,
                38f,
                25,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.70f, 0.80f, 1f));
            routeStatusText = CreateText(
                panel.transform,
                "Quarter2RouteStatusText",
                "계열 상태 준비 중",
                22f,
                62f,
                876f,
                180f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
            panel.SetActive(false);
        }

        private static void CreateQuarter3Panel(
            Transform parent,
            out GameObject panel,
            out Text statusText,
            out Text sourceProgressText,
            out Text resultText,
            out Button sourceAButton,
            out Button sourceBButton,
            out Button openFinalChoiceButton,
            out Button finalOptionAButton,
            out Button finalOptionBButton)
        {
            panel = CreatePanel(
                parent,
                "Quarter3Panel",
                960f,
                170f,
                920f,
                730f,
                new Color(0.08f, 0.105f, 0.075f, 0.995f));
            CreateText(
                panel.transform,
                "Quarter3TitleText",
                "3분기: 단서 수집과 최종 선택",
                22f,
                14f,
                876f,
                42f,
                29,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.78f, 1f, 0.68f));
            statusText = CreateText(
                panel.transform,
                "Quarter3StatusText",
                "3분기 상태 준비 중",
                22f,
                66f,
                876f,
                116f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
            sourceProgressText = CreateText(
                panel.transform,
                "Quarter3SourceProgressText",
                "출처별 진행도 준비 중",
                22f,
                190f,
                876f,
                78f,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.84f, 0.92f, 1f));
            sourceAButton = CreateButton(
                panel.transform,
                "Quarter3SourceAButton",
                "TEST_SOURCE_A 단서 획득",
                22f,
                280f,
                420f,
                58f,
                new Color(0.18f, 0.48f, 0.32f, 1f));
            sourceBButton = CreateButton(
                panel.transform,
                "Quarter3SourceBButton",
                "TEST_SOURCE_B 단서 획득",
                458f,
                280f,
                440f,
                58f,
                new Color(0.18f, 0.42f, 0.50f, 1f));
            openFinalChoiceButton = CreateButton(
                panel.transform,
                "Quarter3OpenFinalChoiceButton",
                "최종 선택 개방",
                22f,
                352f,
                876f,
                58f,
                new Color(0.58f, 0.35f, 0.08f, 1f));
            finalOptionAButton = CreateButton(
                panel.transform,
                "Quarter3FinalOptionAButton",
                "최종 선택 A",
                22f,
                424f,
                420f,
                66f,
                new Color(0.47f, 0.24f, 0.62f, 1f));
            finalOptionBButton = CreateButton(
                panel.transform,
                "Quarter3FinalOptionBButton",
                "최종 선택 B",
                458f,
                424f,
                440f,
                66f,
                new Color(0.55f, 0.22f, 0.38f, 1f));
            resultText = CreateText(
                panel.transform,
                "Quarter3ResultText",
                "3분기 입력 대기 중",
                22f,
                510f,
                876f,
                190f,
                21,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(1f, 0.88f, 0.58f));
            panel.SetActive(false);
        }

        private static void CreateCampaignResultPanel(
            Transform parent,
            out GameObject panel,
            out Text resultText,
            out Button startQuarter3Button)
        {
            panel = CreatePanel(
                parent,
                "CampaignResultPanel",
                960f,
                170f,
                920f,
                450f,
                new Color(0.15f, 0.08f, 0.12f, 0.995f));
            CreateText(
                panel.transform,
                "CampaignResultTitleText",
                "통합 캠페인 결과",
                22f,
                16f,
                876f,
                44f,
                30,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.76f, 0.80f));
            resultText = CreateText(
                panel.transform,
                "CampaignResultText",
                "결과 대기 중",
                22f,
                78f,
                876f,
                250f,
                24,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white);
            startQuarter3Button = CreateButton(
                panel.transform,
                "StartQuarter3Button",
                "3분기 시작",
                22f,
                352f,
                876f,
                72f,
                new Color(0.24f, 0.48f, 0.22f, 1f));
            panel.SetActive(false);
        }

        private static void CreateControlPanel(
            Transform parent,
            out GameObject panel,
            out Toggle fixedRollToggle,
            out InputField fixedRollInput,
            out Text randomStatusText,
            out Button resetButton)
        {
            panel = CreatePanel(
                parent,
                "ControlPanel",
                40f,
                920f,
                1840f,
                140f,
                new Color(0.055f, 0.065f, 0.09f, 0.99f));
            fixedRollToggle = CreateToggle(
                panel.transform,
                "UseFixedRandomRollToggle",
                "고정 randomRoll 사용",
                22f,
                20f,
                300f,
                46f);
            fixedRollToggle.isOn = true;
            fixedRollInput = CreateInputField(
                panel.transform,
                "FixedRandomRollInput",
                "0.0",
                340f,
                18f,
                240f,
                50f);
            randomStatusText = CreateText(
                panel.transform,
                "RandomRollStatusText",
                "고정 randomRoll: 예 | 값: 0.0",
                610f,
                14f,
                760f,
                92f,
                18,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.82f, 0.88f, 0.96f));
            resetButton = CreateButton(
                panel.transform,
                "ResetCampaignButton",
                "전체 초기화",
                1400f,
                20f,
                416f,
                96f,
                new Color(0.45f, 0.18f, 0.24f, 1f));
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
            GameObject panel =
                new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            SetTopLeftRect(
                panel.GetComponent<RectTransform>(),
                x,
                y,
                width,
                height);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static void CreateFullScreenImage(
            Transform parent,
            string name,
            Color color)
        {
            GameObject background =
                new(name, typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            RectTransform rect =
                background.GetComponent<RectTransform>();
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
            GameObject textObject =
                new(name, typeof(RectTransform), typeof(Text));
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
            ApplyButtonColors(button, normalColor);
            Text labelText = CreateText(
                buttonObject.transform,
                "Label",
                label,
                8f,
                5f,
                width - 16f,
                height - 10f,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 13;
            labelText.resizeTextMaxSize = 20;
            return button;
        }

        private static Toggle CreateToggle(
            Transform parent,
            string name,
            string label,
            float x,
            float y,
            float width,
            float height)
        {
            GameObject toggleObject =
                new(name, typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);
            SetTopLeftRect(
                toggleObject.GetComponent<RectTransform>(),
                x,
                y,
                width,
                height);
            Toggle toggle = toggleObject.GetComponent<Toggle>();

            GameObject background = new(
                "Background",
                typeof(RectTransform),
                typeof(Image));
            background.transform.SetParent(toggleObject.transform, false);
            SetTopLeftRect(
                background.GetComponent<RectTransform>(),
                0f,
                4f,
                38f,
                38f);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color =
                new Color(0.18f, 0.20f, 0.25f, 1f);

            GameObject checkmark = new(
                "Checkmark",
                typeof(RectTransform),
                typeof(Image));
            checkmark.transform.SetParent(background.transform, false);
            RectTransform checkRect =
                checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            Image checkImage = checkmark.GetComponent<Image>();
            checkImage.color =
                new Color(0.25f, 0.82f, 0.48f, 1f);

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkImage;
            CreateText(
                toggleObject.transform,
                "Label",
                label,
                50f,
                0f,
                width - 50f,
                height,
                19,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white);
            return toggle;
        }

        private static InputField CreateInputField(
            Transform parent,
            string name,
            string initialValue,
            float x,
            float y,
            float width,
            float height)
        {
            GameObject inputObject = new(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            SetTopLeftRect(
                inputObject.GetComponent<RectTransform>(),
                x,
                y,
                width,
                height);
            Image image = inputObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            Text inputText = CreateText(
                inputObject.transform,
                "Text",
                initialValue,
                12f,
                5f,
                width - 24f,
                height - 10f,
                21,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                Color.white);
            Text placeholder = CreateText(
                inputObject.transform,
                "Placeholder",
                "0 이상 1 미만",
                12f,
                5f,
                width - 24f,
                height - 10f,
                19,
                FontStyle.Italic,
                TextAnchor.MiddleLeft,
                new Color(0.55f, 0.58f, 0.64f, 1f));
            InputField input = inputObject.GetComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.DecimalNumber;
            input.text = initialValue;
            return input;
        }

        private static void ApplyButtonColors(
            Button button,
            Color normalColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor =
                Color.Lerp(normalColor, Color.white, 0.16f);
            colors.pressedColor =
                Color.Lerp(normalColor, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor =
                new Color(0.16f, 0.17f, 0.19f, 0.72f);
            button.colors = colors;
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
