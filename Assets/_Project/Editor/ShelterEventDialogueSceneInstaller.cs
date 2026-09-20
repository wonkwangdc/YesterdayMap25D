using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.Shelter;
using YesterdayMap.UI;

namespace YesterdayMap.EditorTools
{
    // Installs the Shelter door event dialogue UI into the existing Shelter scene.
    public static class ShelterEventDialogueSceneInstaller
    {
        private const string ShelterScenePath = "Assets/_Project/Scenes/Shelter.unity";
        private const string PanelName = "DoorEventDialoguePanel";
        private const string ControllerName = "ShelterEventDialogueController";
        private const string DialogueFrameTexturePath =
            "Assets/_Project/Scenes/UI/textm.png";
        private const string ProtagonistPortraitPath =
            "Assets/_Project/Scenes/UI/Portraits/DialoguePortrait_Protagonist.png";
        private const string RadioPortraitPath =
            "Assets/_Project/Scenes/UI/Portraits/radio poto.png";
        private const string RaiderGroupPortraitPath =
            "Assets/_Project/Scenes/UI/Portraits/DialoguePortrait_RaiderGroup.png";
        private const string SurvivorGroupPortraitPath =
            "Assets/_Project/Scenes/UI/Portraits/DialoguePortrait_SurvivorGroup.png";
        private const string ArmedGroupPortraitPath =
            "Assets/_Project/Scenes/UI/Portraits/armedgroup_red2.png";
        private const string MilitaryRescuePortraitPath =
            "Assets/_Project/Scenes/UI/Portraits/DialoguePortrait_MilitaryRescue.png";
        private const float DialogueUiWidthScale = 1f;
        private const float DialogueUiHeightScale = 0.67f;
        private static readonly Vector2 DialogueUiPivot = new Vector2(0.5f, 0.37f);

        [MenuItem("Yesterday Map/Install Shelter Event Dialogue")]
        public static void Install()
        {
            if (!File.Exists(ShelterScenePath))
            {
                Debug.LogError($"Shelter scene not found: {ShelterScenePath}");
                return;
            }

            if (Application.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before installing Shelter event dialogue UI.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ShelterScenePath)
            {
                EditorSceneManager.OpenScene(ShelterScenePath);
            }

            ConfigurePortraitImports();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("Shelter scene has no Canvas.");
                return;
            }

            ShelterEventDialogueUI dialogueUI = CreateOrUpdateDialoguePanel(canvas.transform);
            ShelterEventDialogueController controller = CreateOrUpdateController(dialogueUI);
            LinkDoor(controller);
            LinkStoryDesk(controller);

            EditorUtility.SetDirty(dialogueUI);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Shelter event dialogue UI installed.");
        }

        private static ShelterEventDialogueUI CreateOrUpdateDialoguePanel(Transform canvas)
        {
            GameObject root = FindSceneObject(PanelName);
            if (root == null)
            {
                root = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.transform.SetParent(canvas, false);
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dim = root.GetComponent<Image>();
            dim.color = new Color(0.005f, 0.005f, 0.004f, 0.24f);
            dim.raycastTarget = true;

            Texture2D dialogueTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(DialogueFrameTexturePath);

            RawImage paper = EnsureRawImage(
                root.transform,
                "DiaryPaper",
                new Vector2(0.16f, 0.035f),
                new Vector2(0.84f, 0.64f));
            paper.texture = dialogueTexture;
            paper.uvRect = new Rect(0f, 0f, 1f, 1f);
            paper.color = Color.white;
            SetFixedRect(paper.rectTransform, new Vector2(-653f, -502f), new Vector2(653f, 151f));

            RawImage speakerPlate = EnsureRawImage(
                root.transform,
                "SpeakerPlateBackground",
                ScaleDialogueAnchor(new Vector2(0.12f, 0.59f)),
                ScaleDialogueAnchor(new Vector2(0.39f, 0.70f)));
            speakerPlate.texture = dialogueTexture;
            speakerPlate.uvRect = new Rect(0.09f, 0.73f, 0.31f, 0.12f);
            speakerPlate.color = Color.white;
            speakerPlate.gameObject.SetActive(false);

            Image speakerTextMask = EnsureImage(
                root.transform,
                "SpeakerTextMask",
                ScaleDialogueAnchor(new Vector2(0.145f, 0.615f)),
                ScaleDialogueAnchor(new Vector2(0.365f, 0.685f)));
            speakerTextMask.color = new Color(0.018f, 0.016f, 0.013f, 0.97f);
            speakerTextMask.gameObject.SetActive(false);

            RawImage firstChoiceFrame = EnsureRawImage(
                root.transform,
                "FirstChoiceFrame",
                ScaleDialogueAnchor(new Vector2(0.12f, 0.04f)),
                ScaleDialogueAnchor(new Vector2(0.49f, 0.17f)));
            firstChoiceFrame.texture = dialogueTexture;
            firstChoiceFrame.uvRect = new Rect(0.13f, 0.105f, 0.37f, 0.125f);
            firstChoiceFrame.color = Color.white;
            firstChoiceFrame.gameObject.SetActive(false);

            RawImage secondChoiceFrame = EnsureRawImage(
                root.transform,
                "SecondChoiceFrame",
                ScaleDialogueAnchor(new Vector2(0.51f, 0.04f)),
                ScaleDialogueAnchor(new Vector2(0.88f, 0.17f)));
            secondChoiceFrame.texture = dialogueTexture;
            secondChoiceFrame.uvRect = new Rect(0.50f, 0.105f, 0.37f, 0.125f);
            secondChoiceFrame.color = Color.white;
            secondChoiceFrame.gameObject.SetActive(false);

            RawImage leftPortrait = EnsureRawImage(
                root.transform,
                "LeftCharacterPortrait",
                new Vector2(0.02f, 0.04f),
                new Vector2(0.02f, 0.96f));
            ConfigurePortrait(
                leftPortrait,
                AssetDatabase.LoadAssetAtPath<Texture2D>(ProtagonistPortraitPath),
                alignRight: false);

            RawImage rightPortrait = EnsureRawImage(
                root.transform,
                "RightCharacterPortrait",
                new Vector2(0.98f, 0.05f),
                new Vector2(0.98f, 0.92f));
            ConfigurePortrait(
                rightPortrait,
                AssetDatabase.LoadAssetAtPath<Texture2D>(SurvivorGroupPortraitPath),
                alignRight: true);
            SetFixedRect(leftPortrait.rectTransform, new Vector2(-922f, -497f), new Vector2(-259.33f, 497f));
            SetFixedRect(rightPortrait.rectTransform, new Vector2(295.33f, -486f), new Vector2(922f, 454f));

            leftPortrait.transform.SetSiblingIndex(0);
            rightPortrait.transform.SetSiblingIndex(1);
            paper.transform.SetSiblingIndex(2);
            speakerPlate.transform.SetSiblingIndex(3);
            speakerTextMask.transform.SetSiblingIndex(4);

            Image box = EnsureImage(
                root.transform,
                "DialogueBox",
                Vector2.zero,
                Vector2.zero);
            box.color = Color.clear;

            Text day = EnsureText(
                root.transform,
                "EventDayText",
                new Vector2(0.70f, 0.46f),
                new Vector2(0.78f, 0.50f),
                ScaleDialogueFont(20),
                TextAnchor.MiddleRight);
            day.color = new Color(0.78f, 0.64f, 0.43f, 0.92f);
            SetFixedRect(day.rectTransform, new Vector2(384f, -43f), new Vector2(538f, 0f));

            Text speaker = EnsureText(
                root.transform,
                "SpeakerText",
                new Vector2(0.20f, 0.48f),
                new Vector2(0.39f, 0.54f),
                ScaleDialogueFont(28),
                TextAnchor.MiddleLeft);
            speaker.color = new Color(0.95f, 0.79f, 0.49f, 1f);
            SetFixedRect(speaker.rectTransform, new Vector2(-502.5f, -21.5f), new Vector2(-137.5f, 43.5f));
            speaker.gameObject.SetActive(false);

            Text title = EnsureText(
                root.transform,
                "EventTitleText",
                new Vector2(0.22f, 0.40f),
                new Vector2(0.78f, 0.45f),
                ScaleDialogueFont(28),
                TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.96f, 0.86f, 0.66f, 1f);
            SetFixedRect(title.rectTransform, new Vector2(-502.5f, -21.5f), new Vector2(-137.5f, 43.5f));

            Text body = EnsureText(
                root.transform,
                "EventBodyText",
                new Vector2(0.22f, 0.24f),
                new Vector2(0.78f, 0.40f),
                ScaleDialogueFont(23),
                TextAnchor.UpperLeft);
            body.color = new Color(0.91f, 0.83f, 0.68f, 1f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            SetFixedRect(body.rectTransform, new Vector2(-538f, -281f), new Vector2(538f, -108f));

            Text result = EnsureText(
                root.transform,
                "EventResultText",
                new Vector2(0.22f, 0.18f),
                new Vector2(0.78f, 0.24f),
                ScaleDialogueFont(22),
                TextAnchor.UpperLeft);
            result.color = new Color(0.98f, 0.74f, 0.39f, 1f);
            result.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetFixedRect(result.rectTransform, new Vector2(-538f, -346f), new Vector2(538f, -281f));

            Button firstButton = EnsureButton(
                root.transform,
                "FirstChoiceButton",
                new Vector2(0.22f, 0.09f),
                new Vector2(0.48f, 0.16f),
                out Text firstText);
            SetFixedRect(firstButton.GetComponent<RectTransform>(), new Vector2(-538f, -433f), new Vector2(-38f, -357f));
            firstText.text = "선택 1";
            Button secondButton = EnsureButton(
                root.transform,
                "SecondChoiceButton",
                new Vector2(0.52f, 0.09f),
                new Vector2(0.78f, 0.16f),
                out Text secondText);
            SetFixedRect(secondButton.GetComponent<RectTransform>(), new Vector2(38f, -433f), new Vector2(538f, -357f));
            secondText.text = "선택 2";
            Button closeButton = EnsureButton(
                root.transform,
                "CloseDialogueButton",
                new Vector2(0.76f, 0.50f),
                new Vector2(0.81f, 0.54f),
                out Text closeText);
            closeText.text = "닫기";
            closeText.fontSize = ScaleDialogueFont(20);
            closeText.color = new Color(0.95f, 0.79f, 0.49f, 1f);
            SetFixedRect(closeButton.GetComponent<RectTransform>(), new Vector2(499f, 0f), new Vector2(595f, 43f));
            Image closeImage = closeButton.GetComponent<Image>();
            closeImage.color = Color.clear;
            ColorBlock closeColors = closeButton.colors;
            closeColors.normalColor = Color.clear;
            closeColors.highlightedColor = new Color(1f, 0.72f, 0.28f, 0.12f);
            closeColors.pressedColor = new Color(1f, 0.62f, 0.20f, 0.20f);
            closeColors.selectedColor = closeColors.highlightedColor;
            closeButton.colors = closeColors;

            ShelterEventDialogueUI dialogue = root.GetComponent<ShelterEventDialogueUI>();
            if (dialogue == null)
            {
                dialogue = root.AddComponent<ShelterEventDialogueUI>();
            }

            GameObject subtitlesRoot = FindActiveSubtitleRoot();
            dialogue.Configure(
                root,
                paper,
                leftPortrait,
                rightPortrait,
                day,
                speaker,
                title,
                body,
                result,
                firstButton,
                secondButton,
                closeButton,
                firstText,
                secondText,
                subtitlesRoot);
            root.SetActive(false);
            return dialogue;
        }

        private static GameObject FindActiveSubtitleRoot()
        {
            UIManager uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uiManager == null)
            {
                return null;
            }

            SerializedObject serializedUi = new SerializedObject(uiManager);
            Text message = serializedUi.FindProperty("messageText")?.objectReferenceValue as Text;
            return message != null && message.transform.parent != null
                ? message.transform.parent.gameObject
                : null;
        }

        private static ShelterEventDialogueController CreateOrUpdateController(ShelterEventDialogueUI dialogueUI)
        {
            GameObject target = FindSceneObject(ControllerName);
            if (target == null)
            {
                GameObject managers = GameObject.Find("ShelterManagers");
                target = new GameObject(ControllerName);
                target.transform.SetParent(managers != null ? managers.transform : null, false);
            }

            ShelterEventDialogueController controller = target.GetComponent<ShelterEventDialogueController>();
            if (controller == null)
            {
                controller = target.AddComponent<ShelterEventDialogueController>();
            }

            DayCycleManager dayCycle = Object.FindFirstObjectByType<DayCycleManager>(FindObjectsInactive.Include);
            UIManager uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            CharacterStats stats = Object.FindFirstObjectByType<CharacterStats>(FindObjectsInactive.Include);
            Texture2D protagonist =
                AssetDatabase.LoadAssetAtPath<Texture2D>(ProtagonistPortraitPath);
            Texture2D radio =
                AssetDatabase.LoadAssetAtPath<Texture2D>(RadioPortraitPath);
            Texture2D raiderGroup =
                AssetDatabase.LoadAssetAtPath<Texture2D>(RaiderGroupPortraitPath);
            Texture2D survivorGroup =
                AssetDatabase.LoadAssetAtPath<Texture2D>(SurvivorGroupPortraitPath);
            Texture2D armedGroup =
                AssetDatabase.LoadAssetAtPath<Texture2D>(ArmedGroupPortraitPath);
            Texture2D militaryRescue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(MilitaryRescuePortraitPath);
            controller.Configure(
                dayCycle,
                uiManager,
                dialogueUI,
                protagonist,
                radio,
                raiderGroup,
                survivorGroup,
                armedGroup,
                militaryRescue,
                stats);
            return controller;
        }

        private static void ConfigurePortraitImports()
        {
            ConfigurePortraitImport(ProtagonistPortraitPath);
            ConfigurePortraitImport(RadioPortraitPath);
            ConfigurePortraitImport(RaiderGroupPortraitPath);
            ConfigurePortraitImport(SurvivorGroupPortraitPath);
            ConfigurePortraitImport(ArmedGroupPortraitPath);
            ConfigurePortraitImport(MilitaryRescuePortraitPath);
        }

        private static void ConfigurePortraitImport(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Dialogue portrait texture not found: {assetPath}");
                return;
            }

            bool changed = false;
            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void LinkDoor(ShelterEventDialogueController controller)
        {
            DoorObject door = Object.FindFirstObjectByType<DoorObject>(FindObjectsInactive.Include);
            if (door == null)
            {
                Debug.LogWarning("DoorObject not found; T event dialogue will not be linked to the door.");
                return;
            }

            SerializedObject serializedDoor = new SerializedObject(door);
            serializedDoor.FindProperty("eventDialogue").objectReferenceValue = controller;
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(door);
        }

        private static void LinkStoryDesk(ShelterEventDialogueController controller)
        {
            GameObject deskObject = FindSceneObject("BunkerFurniture_Desk");
            if (deskObject == null)
            {
                Debug.LogWarning("BunkerFurniture_Desk not found; Q3 signal stories will not be linked to the desk.");
                return;
            }

            DeskStoryObject desk = deskObject.GetComponent<DeskStoryObject>();
            if (desk == null)
            {
                desk = deskObject.AddComponent<DeskStoryObject>();
            }

            SerializedObject serializedDesk = new SerializedObject(desk);
            serializedDesk.FindProperty("eventDialogue").objectReferenceValue = controller;
            serializedDesk.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(desk);
        }

        private static RawImage EnsureRawImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject child = EnsureChild(parent, name, typeof(RawImage));
            RawImage image = child.GetComponent<RawImage>();
            image.raycastTarget = false;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            return image;
        }

        private static void ConfigurePortrait(
            RawImage image,
            Texture2D texture,
            bool alignRight)
        {
            image.texture = texture;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            image.color = Color.white;

            RectTransform rect = image.rectTransform;
            rect.pivot = new Vector2(alignRight ? 1f : 0f, 0.5f);

            AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = texture != null && texture.height > 0
                ? (float)texture.width / texture.height
                : 2f / 3f;
            image.gameObject.SetActive(texture != null);
        }

        private static Image EnsureImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject child = EnsureChild(parent, name, typeof(Image));
            Image image = child.GetComponent<Image>();
            image.raycastTarget = false;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            return image;
        }

        private static Text EnsureText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor alignment)
        {
            GameObject child = EnsureChild(parent, name, typeof(Text));
            Text text = child.GetComponent<Text>();
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.fontSize = size;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(16, size);
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            return text;
        }

        private static Button EnsureButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out Text label)
        {
            GameObject child = EnsureChild(parent, name, typeof(Image), typeof(Button));
            Image image = child.GetComponent<Image>();
            image.color = Color.clear;
            Button button = child.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            label = EnsureText(
                child.transform,
                "Label",
                new Vector2(0.06f, 0.10f),
                new Vector2(0.94f, 0.90f),
                ScaleDialogueFont(25),
                TextAnchor.MiddleCenter);
            label.color = new Color(0.95f, 0.79f, 0.49f, 1f);
            return button;
        }

        private static GameObject EnsureChild(Transform parent, string name, params System.Type[] components)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                foreach (System.Type component in components)
                {
                    if (existing.GetComponent(component) == null)
                    {
                        existing.gameObject.AddComponent(component);
                    }
                }

                return existing.gameObject;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                if (child.GetComponent(component) == null)
                {
                    child.AddComponent(component);
                }
            }

            return child;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void SetFixedRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static Vector2 ScaleDialogueAnchor(Vector2 anchor)
        {
            Vector2 offset = anchor - DialogueUiPivot;
            return DialogueUiPivot + new Vector2(
                offset.x * DialogueUiWidthScale,
                offset.y * DialogueUiHeightScale);
        }

        private static int ScaleDialogueFont(int size)
        {
            return size;
        }
    }
}
