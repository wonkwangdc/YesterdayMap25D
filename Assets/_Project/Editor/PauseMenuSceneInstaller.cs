using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YesterdayMap.Core;
using YesterdayMap.UI;

namespace YesterdayMap.EditorTools
{
    public static class PauseMenuSceneInstaller
    {
        private const string SettingsPrefabPath =
            "Assets/_Project/Prefabs/UI/GameSettingsPanel.prefab";
        private const string PausePrefabPath =
            "Assets/_Project/Prefabs/UI/PauseMenu.prefab";
        private const string ShelterScenePath =
            "Assets/_Project/Scenes/Shelter.unity";
        private const string MainMenuScenePath =
            "Assets/_Project/Scenes/MainMenu.unity";
        private const string PauseBackgroundPath =
            "Assets/_Project/Art/UI/PauseMenu/PauseMenu_Background.png";
        private const string PauseSaveSlotsBackgroundPath =
            "Assets/_Project/Art/UI/PauseMenu/PauseMenu_SaveSlotsBackground.png";
        private const string PauseLoadSlotsBackgroundPath =
            "Assets/_Project/Art/UI/PauseMenu/PauseMenu_LoadSlotsBackground.png";
        private const string SettingsBackgroundPath =
            "Assets/_Project/Art/UI/PauseMenu/SettingsMenu_Background.png";
        private const string BloodHoverOverlayPath =
            "Assets/_Project/Art/UI/PauseMenu/BloodHoverOverlay.png";

        private static readonly Color Backdrop = new(0.025f, 0.03f, 0.032f, 0.86f);
        private static readonly Color Panel = new(0.10f, 0.11f, 0.105f, 0.98f);
        private static readonly Color ButtonNormal = new(0.20f, 0.24f, 0.22f, 1f);
        private static readonly Color ButtonHover = new(0.30f, 0.40f, 0.35f, 1f);
        private static readonly Color Accent = new(0.72f, 0.60f, 0.35f, 1f);
        private static readonly Color TextColor = new(0.92f, 0.90f, 0.82f, 1f);
        private static readonly Color PauseButtonNormal = new(0.105f, 0.085f, 0.06f, 0.97f);
        private static readonly Color PauseButtonHover = new(0.23f, 0.27f, 0.18f, 1f);
        private static readonly Color PauseTextColor = new(0.86f, 0.79f, 0.65f, 1f);
        private static readonly Color Ink = new(0.12f, 0.085f, 0.045f, 1f);

        [MenuItem("Tools/YesterdayMap/Install Pause And Save UI")]
        public static void Install()
        {
            string originalScene = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("현재 Scene에 저장되지 않은 변경이 있어 Pause UI 설치를 중단했습니다.");
                return;
            }

            Directory.CreateDirectory("Assets/_Project/Prefabs/UI");
            ConfigurePauseTexture(PauseBackgroundPath);
            ConfigurePauseTexture(PauseSaveSlotsBackgroundPath);
            ConfigurePauseTexture(PauseLoadSlotsBackgroundPath);
            ConfigurePauseTexture(SettingsBackgroundPath);
            ConfigureBloodHoverTexture();
            Sprite bloodHoverSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(BloodHoverOverlayPath);
            if (bloodHoverSprite == null)
                throw new MissingReferenceException(
                    "Blood hover overlay sprite was not found.");

            GameObject settingsPrefab =
                BuildReleaseSettingsPrefab(bloodHoverSprite);
            GameObject pausePrefab =
                BuildReleasePausePrefab(settingsPrefab, bloodHoverSprite);
            InstallInShelter(pausePrefab);
            InstallInMainMenu(settingsPrefab);

            if (!string.IsNullOrWhiteSpace(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Pause, save/load, and settings UI installation completed.");
        }

        private static void ConfigurePauseTexture(string assetPath)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new MissingReferenceException(
                    $"Pause texture importer was not found: {assetPath}");

            bool changed = importer.npotScale != TextureImporterNPOTScale.None ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!changed) return;

            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureBloodHoverTexture()
        {
            AssetDatabase.ImportAsset(
                BloodHoverOverlayPath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(BloodHoverOverlayPath) as TextureImporter;
            if (importer == null)
                throw new MissingReferenceException(
                    $"Blood hover texture importer was not found: {BloodHoverOverlayPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static GameObject BuildReleaseSettingsPrefab(
            Sprite bloodHoverSprite)
        {
            GameObject root = CreateRect("GameSettingsController", null);
            root.AddComponent<KoreanFontApplicator>();
            GameSettingsPanelUI settings = root.AddComponent<GameSettingsPanelUI>();

            GameObject panelRoot = CreateRect("SettingsPanel", root.transform);
            Stretch(panelRoot.GetComponent<RectTransform>());
            // Keep the scene visible behind the settings window. A solid black
            // fallback produced large black bands on non-16:9 displays.
            AddImage(panelRoot, new Color(0f, 0f, 0f, 0.08f));
            Texture2D backgroundTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(SettingsBackgroundPath);
            if (backgroundTexture == null)
                throw new MissingReferenceException("Settings menu background was not found.");
            GameObject artwork = CreateCroppedArtwork(
                "SettingsArtwork",
                panelRoot.transform,
                backgroundTexture,
                new Vector2(950f, 890f),
                new Rect(0.2536f, 0.0978f, 0.4928f, 0.8225f));
            artwork.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(360f, 0f);

            GameObject window = CreateRect("SettingsWindow", artwork.transform);
            Stretch(window.GetComponent<RectTransform>());

            Color controlMask = new(0.055f, 0.050f, 0.043f, 1f);
            AddControlMask("VolumeMask", window.transform,
                new Vector2(145f, 251f), new Vector2(425f, 64f), controlMask);
            Slider volume = CreateSettingsSlider("VolumeSlider", window.transform,
                new Vector2(145f, 251f), new Vector2(402f, 48f));

            AddControlMask("MouseSensitivityMask", window.transform,
                new Vector2(145f, 177f), new Vector2(425f, 64f), controlMask);
            Slider mouseSensitivity = CreateSettingsSlider("MouseSensitivitySlider", window.transform,
                new Vector2(145f, 177f), new Vector2(402f, 48f));

            // The source artwork contains an already-open screen-mode list.
            // Replace that entire lower control region so the real dropdown only
            // appears while the player is interacting with it.
            AddControlMask(
                "SettingsLowerMask",
                window.transform,
                new Vector2(0f, -70f),
                new Vector2(820f, 390f),
                controlMask);

            CreateText("DisplayModeLabel", window.transform, "화면 모드", 25,
                TextAnchor.MiddleLeft, new Vector2(-280f, 70f),
                new Vector2(230f, 46f), TextColor);
            Dropdown displayMode = CreateSettingsDropdown("DisplayModeDropdown", window.transform,
                new Vector2(145f, 70f), new Vector2(402f, 60f));

            CreateText("ResolutionLabel", window.transform, "해상도", 25,
                TextAnchor.MiddleLeft, new Vector2(-280f, -30f),
                new Vector2(230f, 46f), TextColor);
            Dropdown resolution = CreateSettingsDropdown("ResolutionDropdown", window.transform,
                new Vector2(145f, -30f), new Vector2(402f, 60f));

            CreateText("MovementPresetLabel", window.transform, "이동 키", 25,
                TextAnchor.MiddleLeft, new Vector2(-280f, -130f),
                new Vector2(230f, 46f), TextColor);
            Dropdown movementPreset = CreateSettingsDropdown("MovementPresetDropdown", window.transform,
                new Vector2(145f, -130f), new Vector2(402f, 60f));

            CreateText("ReduceFlashingLabel", window.transform, "화면 깜빡임 감소", 25,
                TextAnchor.MiddleLeft, new Vector2(-280f, -220f),
                new Vector2(300f, 46f), TextColor);
            Toggle reduceFlashing = CreateToggle("ReduceFlashingToggle", window.transform,
                new Vector2(145f, -220f));

            // Keep the close button already painted into the artwork and place
            // only a real transparent hit target over it.
            Button close = CreateHotspotButton("CloseButton", window.transform,
                new Vector2(0f, -300f), new Vector2(350f, 68f));
            AttachBloodHoverEffect(close, bloodHoverSprite);
            settings.Configure(panelRoot, volume, mouseSensitivity, displayMode,
                resolution, movementPreset, close, reduceFlashing);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SettingsPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddControlMask(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject mask = CreateRect(name, parent);
            RectTransform rect = mask.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Image image = AddImage(mask, color);
            image.raycastTarget = false;
        }

        private static Slider CreateSettingsSlider(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            Slider slider = CreateSlider(name, parent, position, size);
            RectTransform background =
                slider.transform.Find("Background").GetComponent<RectTransform>();
            background.offsetMin = new Vector2(0f, -6f);
            background.offsetMax = new Vector2(0f, 6f);
            RectTransform fillArea =
                slider.transform.Find("Fill Area").GetComponent<RectTransform>();
            fillArea.offsetMin = new Vector2(5f, 18f);
            fillArea.offsetMax = new Vector2(-5f, -18f);
            RectTransform handle =
                slider.transform.Find("Handle Slide Area/Handle").GetComponent<RectTransform>();
            handle.sizeDelta = new Vector2(12f, 0f);
            return slider;
        }

        private static Dropdown CreateSettingsDropdown(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            Dropdown dropdown = CreateDropdown(name, parent, position, size);
            Image image = dropdown.GetComponent<Image>();
            image.color = Color.white;
            Outline outline = dropdown.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.27f, 0.18f, 0.08f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            ColorBlock colors = dropdown.colors;
            colors.normalColor = PauseButtonNormal;
            colors.highlightedColor = PauseButtonHover;
            colors.pressedColor = new Color(0.34f, 0.22f, 0.09f, 1f);
            colors.selectedColor = PauseButtonHover;
            colors.disabledColor = new Color(0.10f, 0.09f, 0.075f, 0.60f);
            colors.fadeDuration = 0.08f;
            dropdown.colors = colors;

            Transform item = dropdown.transform.Find("Template/Viewport/Content/Item");
            if (item != null)
            {
                Image itemImage = item.GetComponent<Image>();
                itemImage.color = Color.white;
                Toggle itemToggle = item.GetComponent<Toggle>();
                ColorBlock itemColors = itemToggle.colors;
                itemColors.normalColor = PauseButtonNormal;
                itemColors.highlightedColor = PauseButtonHover;
                itemColors.pressedColor = new Color(0.34f, 0.22f, 0.09f, 1f);
                itemColors.selectedColor = PauseButtonHover;
                itemToggle.colors = itemColors;
            }

            return dropdown;
        }

        private static GameObject BuildReleasePausePrefab(
            GameObject settingsPrefab,
            Sprite bloodHoverSprite)
        {
            GameObject root = CreateRect("PauseMenu", null);
            root.AddComponent<KoreanFontApplicator>();
            PauseMenuUI pause = root.AddComponent<PauseMenuUI>();

            GameObject menuRoot = CreateRect("PausePanel", root.transform);
            Stretch(menuRoot.GetComponent<RectTransform>());
            AddImage(menuRoot, Color.black);
            Texture2D backgroundTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(PauseBackgroundPath);
            if (backgroundTexture == null)
                throw new MissingReferenceException("Pause menu background was not found.");
            GameObject mainArtwork = CreateFittedArtwork(
                "MainArtwork",
                menuRoot.transform,
                backgroundTexture);

            GameObject window = CreateRect("PauseWindow", mainArtwork.transform);
            Stretch(window.GetComponent<RectTransform>());

            // The supplied 16:9 artwork already contains the title and button visuals.
            // These transparent buttons preserve real interaction without duplicating text.
            Button resume = CreateHotspotButton("ContinueButton", window.transform,
                new Vector2(0f, 155f), new Vector2(525f, 74f));
            Button save = CreateHotspotButton("SaveButton", window.transform,
                new Vector2(0f, 68f), new Vector2(525f, 74f));
            Button load = CreateHotspotButton("LoadButton", window.transform,
                new Vector2(0f, -18f), new Vector2(525f, 74f));
            Button settings = CreateHotspotButton("SettingsButton", window.transform,
                new Vector2(0f, -105f), new Vector2(525f, 74f));
            Button mainMenu = CreateHotspotButton("MainMenuButton", window.transform,
                new Vector2(0f, -192f), new Vector2(525f, 74f));
            Button quit = CreateHotspotButton("QuitButton", window.transform,
                new Vector2(0f, -279f), new Vector2(525f, 74f));
            AttachBloodHoverEffect(resume, bloodHoverSprite);
            AttachBloodHoverEffect(save, bloodHoverSprite);
            AttachBloodHoverEffect(load, bloodHoverSprite);
            AttachBloodHoverEffect(settings, bloodHoverSprite);
            AttachBloodHoverEffect(mainMenu, bloodHoverSprite);
            AttachBloodHoverEffect(quit, bloodHoverSprite);
            Text status = CreateText("StatusText", window.transform,
                "벙커에서 현재 진행을 저장할 수 있습니다.", 18,
                TextAnchor.MiddleCenter, new Vector2(0f, -385f), new Vector2(610f, 52f), Ink);

            GameObject slotPanel = CreateRect("SlotSelectionPanel", menuRoot.transform);
            Stretch(slotPanel.GetComponent<RectTransform>());
            AddImage(slotPanel, Color.black);
            Texture2D saveSlotsBackground =
                AssetDatabase.LoadAssetAtPath<Texture2D>(PauseSaveSlotsBackgroundPath);
            Texture2D loadSlotsBackground =
                AssetDatabase.LoadAssetAtPath<Texture2D>(PauseLoadSlotsBackgroundPath);
            if (saveSlotsBackground == null || loadSlotsBackground == null)
                throw new MissingReferenceException("Pause slot backgrounds were not found.");
            GameObject slotsArtwork = CreateFittedArtwork(
                "SlotsArtwork",
                slotPanel.transform,
                saveSlotsBackground);
            RawImage slotsArtworkImage = slotsArtwork.GetComponent<RawImage>();

            GameObject slotWindow = CreateRect("SlotWindow", slotsArtwork.transform);
            Stretch(slotWindow.GetComponent<RectTransform>());
            Button[] slotButtons = new Button[SaveGameManager.SlotCount];
            Text[] slotLabels = new Text[SaveGameManager.SlotCount];
            float[] slotY = { 235f, 115f, -5f, -126f, -246f };
            for (int slot = 0; slot < SaveGameManager.SlotCount; slot++)
            {
                slotButtons[slot] = CreateArtworkSlotButton(
                    $"Slot{slot + 1}Button",
                    slotWindow.transform,
                    $"{slot + 1}번 데이터 - 비어 있음",
                    new Vector2(9f, slotY[slot]),
                    new Vector2(763f, 95f));
                AttachBloodHoverEffect(slotButtons[slot], bloodHoverSprite);
                slotLabels[slot] = slotButtons[slot].transform
                    .Find("Label").GetComponent<Text>();
            }
            Button slotBack = CreateHotspotButton("BackButton", slotWindow.transform,
                new Vector2(4f, -359f), new Vector2(272f, 80f));
            AttachBloodHoverEffect(slotBack, bloodHoverSprite);

            GameObject confirmationPanel =
                CreateRect("SlotConfirmationPanel", menuRoot.transform);
            Stretch(confirmationPanel.GetComponent<RectTransform>());
            AddImage(confirmationPanel, new Color(0f, 0f, 0f, 0.78f));

            GameObject confirmationWindow =
                CreateRect("ConfirmationWindow", confirmationPanel.transform);
            SetCentered(
                confirmationWindow.GetComponent<RectTransform>(),
                660f,
                310f);
            AddImage(confirmationWindow, new Color(0.055f, 0.050f, 0.043f, 1f));
            Outline confirmationOutline =
                confirmationWindow.AddComponent<Outline>();
            confirmationOutline.effectColor =
                new Color(0.30f, 0.19f, 0.075f, 1f);
            confirmationOutline.effectDistance = new Vector2(4f, -4f);

            CreateText("Title", confirmationWindow.transform, "확인", 34,
                TextAnchor.MiddleCenter, new Vector2(0f, 102f),
                new Vector2(560f, 54f), Accent);
            Text confirmationMessage = CreateText(
                "Message",
                confirmationWindow.transform,
                "저장하시겠습니까?",
                29,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 30f),
                new Vector2(570f, 105f),
                PauseTextColor);
            Button confirmButton = CreateButton(
                "ConfirmButton",
                confirmationWindow.transform,
                "예",
                new Vector2(-135f, -91f),
                new Vector2(230f, 66f),
                true);
            Button cancelButton = CreateButton(
                "CancelButton",
                confirmationWindow.transform,
                "아니오",
                new Vector2(135f, -91f),
                new Vector2(230f, 66f),
                true);
            ApplyBloodButtonColors(confirmButton);
            ApplyBloodButtonColors(cancelButton);
            AttachBloodHoverEffect(confirmButton, bloodHoverSprite);
            AttachBloodHoverEffect(cancelButton, bloodHoverSprite);

            GameObject settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab);
            settingsInstance.transform.SetParent(root.transform, false);
            Stretch(settingsInstance.GetComponent<RectTransform>());
            GameSettingsPanelUI settingsPanel = settingsInstance.GetComponent<GameSettingsPanelUI>();
            pause.Configure(menuRoot, resume, save, load, null, settings, mainMenu, quit,
                status, settingsPanel, null);
            pause.ConfigureSlotSelection(
                mainArtwork,
                slotPanel,
                null,
                slotButtons,
                slotLabels,
                slotBack);
            pause.ConfigureSlotArtwork(
                slotsArtworkImage,
                saveSlotsBackground,
                loadSlotsBackground);
            pause.ConfigureSlotConfirmation(
                confirmationPanel,
                confirmationMessage,
                confirmButton,
                cancelButton);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildSettingsPrefab()
        {
            GameObject root = CreateRect("GameSettingsController", null);
            root.AddComponent<KoreanFontApplicator>();
            GameSettingsPanelUI settings = root.AddComponent<GameSettingsPanelUI>();

            GameObject panelRoot = CreateRect("SettingsPanel", root.transform);
            Stretch(panelRoot.GetComponent<RectTransform>());
            AddImage(panelRoot, Backdrop);

            GameObject window = CreateRect("SettingsWindow", panelRoot.transform);
            SetCentered(window.GetComponent<RectTransform>(), 760f, 590f);
            AddImage(window, Panel);

            CreateText("Title", window.transform, "설정", 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 225f), new Vector2(620f, 70f), Accent);
            CreateText("VolumeLabel", window.transform, "전체 음량", 25,
                TextAnchor.MiddleLeft, new Vector2(-210f, 125f), new Vector2(220f, 44f), TextColor);
            Slider volume = CreateSlider("VolumeSlider", window.transform,
                new Vector2(120f, 125f), new Vector2(330f, 42f));

            CreateText("FullScreenLabel", window.transform, "전체 화면", 25,
                TextAnchor.MiddleLeft, new Vector2(-210f, 35f), new Vector2(220f, 44f), TextColor);
            Toggle fullScreen = CreateToggle("FullScreenToggle", window.transform,
                new Vector2(20f, 35f));

            CreateText("ResolutionLabel", window.transform, "해상도", 25,
                TextAnchor.MiddleLeft, new Vector2(-210f, -55f), new Vector2(220f, 44f), TextColor);
            Dropdown resolution = CreateDropdown("ResolutionDropdown", window.transform,
                new Vector2(115f, -55f), new Vector2(340f, 52f));

            Button close = CreateButton("CloseButton", window.transform, "닫기",
                new Vector2(0f, -205f), new Vector2(300f, 58f));
            settings.Configure(panelRoot, volume, null, fullScreen, resolution, null, close);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SettingsPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildPausePrefab(GameObject settingsPrefab)
        {
            GameObject root = CreateRect("PauseMenu", null);
            root.AddComponent<KoreanFontApplicator>();
            PauseMenuUI pause = root.AddComponent<PauseMenuUI>();

            GameObject menuRoot = CreateRect("PausePanel", root.transform);
            Stretch(menuRoot.GetComponent<RectTransform>());
            AddImage(menuRoot, Backdrop);

            GameObject window = CreateRect("PauseWindow", menuRoot.transform);
            SetCentered(window.GetComponent<RectTransform>(), 650f, 820f);
            AddImage(window, Panel);

            CreateText("Title", window.transform, "일시정지", 44,
                TextAnchor.MiddleCenter, new Vector2(0f, 325f), new Vector2(520f, 70f), Accent);

            Button resume = CreateButton("ContinueButton", window.transform, "계속하기",
                new Vector2(0f, 225f), new Vector2(430f, 62f));
            Button save = CreateButton("SaveButton", window.transform, "저장하기",
                new Vector2(0f, 145f), new Vector2(430f, 62f));
            Button load = CreateButton("LoadButton", window.transform, "불러오기",
                new Vector2(0f, 65f), new Vector2(430f, 62f));
            Button settings = CreateButton("SettingsButton", window.transform, "설정",
                new Vector2(0f, -15f), new Vector2(430f, 62f));
            Button mainMenu = CreateButton("MainMenuButton", window.transform, "메인 메뉴로",
                new Vector2(0f, -95f), new Vector2(430f, 62f));
            Button quit = CreateButton("QuitButton", window.transform, "게임 종료",
                new Vector2(0f, -175f), new Vector2(430f, 62f));
            Text status = CreateText("StatusText", window.transform,
                "벙커에서 현재 진행을 저장할 수 있습니다.", 18,
                TextAnchor.MiddleCenter, new Vector2(0f, -275f), new Vector2(520f, 72f),
                new Color(0.72f, 0.75f, 0.70f, 1f));

            GameObject settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab);
            settingsInstance.transform.SetParent(root.transform, false);
            Stretch(settingsInstance.GetComponent<RectTransform>());
            GameSettingsPanelUI settingsPanel =
                settingsInstance.GetComponent<GameSettingsPanelUI>();
            pause.Configure(menuRoot, resume, save, load, null, settings, mainMenu, quit,
                status, settingsPanel, null);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void InstallInShelter(GameObject pausePrefab)
        {
            EditorSceneManager.OpenScene(ShelterScenePath, OpenSceneMode.Single);
            GameObject hud = GameObject.Find("ShelterHUD");
            if (hud == null)
                throw new MissingReferenceException("ShelterHUD was not found.");

            Transform existing = hud.transform.Find("PauseMenu");
            GameObject instance = existing != null
                ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(
                    pausePrefab,
                    hud.scene);
            instance.name = "PauseMenu";
            instance.transform.SetParent(hud.transform, false);
            Stretch(instance.GetComponent<RectTransform>());
            instance.transform.SetAsLastSibling();

            List<GameObject> blockers = new();
            string[] directPanelNames =
            {
                "ExplorationPanel", "StorageInventoryPanel", "WorkProgressPanel",
                "EndingPanel", "DiaryViewPanel", "DoorEventDialoguePanel",
                "SleepConfirmPanel"
            };
            foreach (string panelName in directPanelNames)
            {
                foreach (Transform child in hud.transform)
                {
                    if (child.name == panelName)
                        blockers.Add(child.gameObject);
                }
            }

            Transform admin = hud.transform.Find("AdminDebugUI/AdminPanel");
            if (admin != null) blockers.Add(admin.gameObject);
            instance.GetComponent<PauseMenuUI>().SetBlockingPanels(blockers.ToArray());

            EditorSceneManager.MarkSceneDirty(hud.scene);
            EditorSceneManager.SaveScene(hud.scene);
        }

        private static void InstallInMainMenu(GameObject settingsPrefab)
        {
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
                throw new MissingReferenceException("MainMenuController was not found.");

            Canvas canvas = controller.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new MissingReferenceException("MainMenu Canvas was not found.");

            Transform existing = canvas.transform.Find("GameSettingsController");
            GameObject instance = existing != null
                ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(
                    settingsPrefab,
                    canvas.gameObject.scene);
            instance.name = "GameSettingsController";
            instance.transform.SetParent(canvas.transform, false);
            Stretch(instance.GetComponent<RectTransform>());
            instance.transform.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            if (parent != null) gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Image AddImage(GameObject gameObject, Color color)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static GameObject CreateFittedArtwork(
            string name,
            Transform parent,
            Texture2D texture)
        {
            GameObject artwork = CreateRect(name, parent);
            Stretch(artwork.GetComponent<RectTransform>());
            RawImage image = artwork.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = true;
            AspectRatioFitter fitter = artwork.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = texture.width / (float)texture.height;
            return artwork;
        }

        private static GameObject CreateCroppedArtwork(
            string name,
            Transform parent,
            Texture2D texture,
            Vector2 size,
            Rect uvRect)
        {
            GameObject artwork = CreateRect(name, parent);
            RectTransform rect = artwork.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            RawImage image = artwork.AddComponent<RawImage>();
            image.texture = texture;
            image.uvRect = uvRect;
            image.color = Color.white;
            image.raycastTarget = true;
            return artwork;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject gameObject = CreateRect(name, parent);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Text text = gameObject.AddComponent<Text>();
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            bool pauseStyle = false)
        {
            GameObject gameObject = CreateRect(name, parent);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Color normalColor = pauseStyle ? PauseButtonNormal : ButtonNormal;
            Color hoverColor = pauseStyle ? PauseButtonHover : ButtonHover;
            Color labelColor = pauseStyle ? PauseTextColor : TextColor;
            Image image = AddImage(gameObject, pauseStyle ? Color.white : normalColor);
            if (pauseStyle)
            {
                Outline outline = gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.22f, 0.14f, 0.065f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.pressedColor = new Color(0.16f, 0.20f, 0.18f, 1f);
            colors.selectedColor = hoverColor;
            colors.disabledColor = new Color(0.13f, 0.14f, 0.13f, 0.65f);
            button.colors = colors;
            CreateText("Label", gameObject.transform, label, pauseStyle ? 30 : 25,
                TextAnchor.MiddleCenter, Vector2.zero, size - new Vector2(28f, 8f), labelColor);
            return button;
        }

        private static Button CreateArtworkSlotButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Button button = root.AddComponent<Button>();

            GameObject contentMask = CreateRect("ContentMask", root.transform);
            RectTransform maskRect = contentMask.GetComponent<RectTransform>();
            SetCentered(maskRect, size.x - 42f, size.y - 28f);
            Image maskImage = AddImage(
                contentMask,
                new Color(0.045f, 0.040f, 0.034f, 1f));
            maskImage.raycastTarget = true;

            Text text = CreateText(
                "Label",
                root.transform,
                label,
                31,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                size - new Vector2(56f, 22f),
                PauseTextColor);
            text.raycastTarget = false;

            GameObject hover = CreateRect("HoverOverlay", root.transform);
            Stretch(hover.GetComponent<RectTransform>());
            Image hoverImage = AddImage(hover, Color.white);
            hoverImage.raycastTarget = false;
            button.targetGraphic = hoverImage;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
            colors.highlightedColor = colors.normalColor;
            colors.pressedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0f, 0f, 0f, 0.16f);
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            return button;
        }

        private static Button CreateHotspotButton(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = CreateRect(name, parent);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Image image = AddImage(gameObject, Color.white);
            image.raycastTarget = true;
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            // 별도 이미지 없이 어두운 적혈색이 천천히 스며드는 선택 피드백을 사용한다.
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
            colors.highlightedColor = colors.normalColor;
            colors.pressedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0f, 0f, 0f, 0.10f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            return button;
        }

        private static void AttachBloodHoverEffect(
            Button button,
            Sprite bloodHoverSprite)
        {
            if (button == null || bloodHoverSprite == null) return;

            GameObject overlayObject =
                CreateRect("BloodHoverOverlay", button.transform);
            RectTransform overlayRect =
                overlayObject.GetComponent<RectTransform>();
            Stretch(overlayRect);
            Image overlay = AddImage(overlayObject, Color.white);
            overlay.sprite = bloodHoverSprite;
            overlay.type = Image.Type.Simple;
            overlay.fillAmount = 1f;
            overlay.preserveAspect = false;
            overlay.raycastTarget = false;
            overlay.enabled = false;

            if (button.GetComponent<RectMask2D>() == null)
                button.gameObject.AddComponent<RectMask2D>();

            for (int index = 0; index < button.transform.childCount; index++)
            {
                Transform child = button.transform.GetChild(index);
                if (child == overlayObject.transform) continue;
                if (child.GetComponent<Text>() == null) continue;
                overlayObject.transform.SetSiblingIndex(child.GetSiblingIndex());
                break;
            }

            BloodHoverEffect effect =
                button.gameObject.AddComponent<BloodHoverEffect>();
            effect.Configure(overlay, 0.6f, 0.18f, 0.78f);
        }

        private static void ApplyBloodButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = PauseButtonNormal;
            colors.highlightedColor = new Color(0.38f, 0.018f, 0.024f, 0.82f);
            colors.pressedColor = new Color(0.58f, 0.012f, 0.018f, 0.92f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.18f;
            button.colors = colors;
        }

        private static Slider CreateSlider(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject background = CreateRect("Background", root.transform);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -5f);
            backgroundRect.offsetMax = new Vector2(0f, 5f);
            AddImage(background, new Color(0.05f, 0.06f, 0.055f, 1f));

            GameObject fillArea = CreateRect("Fill Area", root.transform);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(5f, 13f);
            fillAreaRect.offsetMax = new Vector2(-5f, -13f);
            GameObject fill = CreateRect("Fill", fillArea.transform);
            Stretch(fill.GetComponent<RectTransform>());
            Image fillImage = AddImage(fill, Accent);

            GameObject handleArea = CreateRect("Handle Slide Area", root.transform);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRect.offsetMin = new Vector2(10f, -12f);
            handleAreaRect.offsetMax = new Vector2(-10f, 12f);
            GameObject handle = CreateRect("Handle", handleArea.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            SetCentered(handleRect, 12f, 24f);
            Image handleImage = AddImage(handle, TextColor);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            handleRect.sizeDelta = new Vector2(12f, 0f);
            return slider;
        }

        private static Toggle CreateToggle(string name, Transform parent, Vector2 position)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetCentered(rect, 56f, 48f);
            rect.anchoredPosition = position;
            Toggle toggle = root.AddComponent<Toggle>();

            GameObject background = CreateRect("Background", root.transform);
            SetCentered(background.GetComponent<RectTransform>(), 36f, 36f);
            Image backgroundImage = AddImage(background, ButtonNormal);
            GameObject checkmark = CreateRect("Checkmark", background.transform);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            Stretch(checkRect);
            checkRect.offsetMin = new Vector2(7f, 7f);
            checkRect.offsetMax = new Vector2(-7f, -7f);
            Image checkImage = AddImage(checkmark, Accent);

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkImage;
            return toggle;
        }

        private static Dropdown CreateDropdown(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetCentered(rect, size.x, size.y);
            rect.anchoredPosition = position;
            Image rootImage = AddImage(root, ButtonNormal);
            Dropdown dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = rootImage;

            Text caption = CreateText("Label", root.transform, string.Empty, 22,
                TextAnchor.MiddleLeft, new Vector2(-10f, 0f),
                new Vector2(size.x - 60f, size.y), TextColor);
            Text arrow = CreateText("Arrow", root.transform, "▼", 18,
                TextAnchor.MiddleCenter, new Vector2(size.x * 0.5f - 28f, 0f),
                new Vector2(40f, size.y), Accent);
            arrow.raycastTarget = false;

            GameObject template = CreateRect("Template", root.transform);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 170f);
            AddImage(template, Panel);
            template.AddComponent<CanvasGroup>();
            ScrollRect scroll = template.AddComponent<ScrollRect>();

            GameObject viewport = CreateRect("Viewport", template.transform);
            Stretch(viewport.GetComponent<RectTransform>());
            Image viewportImage = AddImage(viewport, Color.white);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateRect("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 50f);

            GameObject item = CreateRect("Item", content.transform);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 50f);
            Toggle itemToggle = item.AddComponent<Toggle>();
            Image itemBackground = AddImage(item, ButtonNormal);
            GameObject checkmark = CreateRect("Item Checkmark", item.transform);
            Stretch(checkmark.GetComponent<RectTransform>());
            Image checkImage = AddImage(checkmark, new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            Text itemText = CreateText("Item Label", item.transform, "Option", 20,
                TextAnchor.MiddleLeft, Vector2.zero, new Vector2(size.x - 30f, 46f), TextColor);
            itemToggle.targetGraphic = itemBackground;
            itemToggle.graphic = checkImage;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            template.SetActive(false);
            return dropdown;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
