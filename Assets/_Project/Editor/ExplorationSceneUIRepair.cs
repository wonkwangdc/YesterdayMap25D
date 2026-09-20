#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Exploration;
using YesterdayMap.Resources;

namespace YesterdayMap.Editor
{
    public static class ExplorationSceneUIRepair
    {
        private const string ScenePath = "Assets/_Project/Scenes/Exploration.unity";
        private const string ExplorationFontPath =
            "Assets/_Project/Scenes/font/UhBee ibuson.ttf";
        private const string Weapon1IconPath =
            "Assets/_Project/Scenes/UI/item_icon/weapon1.png";
        private const string Weapon2IconPath =
            "Assets/_Project/Scenes/UI/item_icon/weapon2.png";
        private const string ExplorationResultBackgroundPath =
            "Assets/_Project/Scenes/UI/Exresuit.png";
        private const float MapAspect = 16f / 9f;

        [MenuItem("Yesterday Map/Repair Exploration Responsive UI")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject canvasObject = FindInScene(scene, "ExplorationUI");
            GameObject controllerObject = FindInScene(scene, "ExplorationController");
            if (canvasObject == null || controllerObject == null) return;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform mapContent = canvasObject.transform.Find("MapContent") as RectTransform;
            if (mapContent == null)
            {
                mapContent = new GameObject("MapContent", typeof(RectTransform), typeof(AspectRatioFitter))
                    .GetComponent<RectTransform>();
                mapContent.SetParent(canvasObject.transform, false);
            }
            mapContent.anchorMin = Vector2.zero;
            mapContent.anchorMax = Vector2.one;
            mapContent.offsetMin = Vector2.zero;
            mapContent.offsetMax = Vector2.zero;
            AspectRatioFitter fitter = mapContent.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = MapAspect;
            mapContent.SetAsFirstSibling();

            string[] alignedNames =
            {
                "ExplorationMap", "ConvenienceStoreButton", "HospitalButton", "ResidentialAreaButton",
                "ConvenienceStoreHoverIcon", "HospitalHoverIcon", "PoliceStationHotspot",
                "PoliceStationHoverIcon", "CommunicationsStationHotspot",
                "CommunicationsStationHoverIcon", "ResidentialAreaHoverIcon", "LocationTitlePanel"
            };
            foreach (string objectName in alignedNames)
            {
                GameObject target = FindInScene(scene, objectName);
                if (target != null && target.transform.parent != mapContent)
                    target.transform.SetParent(mapContent, false);
            }
            GameObject map = FindInScene(scene, "ExplorationMap");
            if (map != null) map.transform.SetAsFirstSibling();

            RectTransform note = EnsureNotePanel(mapContent, scene);
            CanvasGroup noteCanvasGroup = note.GetComponent<CanvasGroup>();
            if (noteCanvasGroup != null)
            {
                noteCanvasGroup.interactable = true;
                noteCanvasGroup.blocksRaycasts = true;
            }
            Text titleText = EnsureText(note, "LocationTitle", string.Empty,
                34, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.84f));
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;

            Text selectionText = EnsureText(note, "SelectedLocationText", "탐사 장소를\n선택하세요",
                20, new Vector2(0.07f, 0.43f), new Vector2(0.93f, 0.74f));
            selectionText.fontStyle = FontStyle.Normal;
            selectionText.alignment = TextAnchor.MiddleCenter;
            selectionText.lineSpacing = 1.1f;
            selectionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            selectionText.verticalOverflow = VerticalWrapMode.Truncate;
            selectionText.resizeTextForBestFit = true;
            selectionText.resizeTextMinSize = 14;
            selectionText.resizeTextMaxSize = 20;
            Text equipmentStatus = EnsureText(note, "EquipmentAvailabilityText", "장비 사용불가",
                18, new Vector2(0.12f, 0.44f), new Vector2(0.88f, 0.49f));
            equipmentStatus.alignment = TextAnchor.MiddleCenter;
            Dropdown equipmentDropdown = EnsureDropdown(note, "EquipmentDropdown",
                new Vector2(0.18f, 0.31f), new Vector2(0.82f, 0.40f));
            Button exploreButton = EnsureButton(note, "ExploreButton", "탐사하기",
                new Vector2(0.18f, 0.1f), new Vector2(0.82f, 0.27f));
            Image exploreBackground = exploreButton.GetComponent<Image>();
            if (exploreBackground != null) exploreBackground.color = Color.clear;
            exploreButton.transition = Selectable.Transition.None;
            titleText.transform.SetAsFirstSibling();
            selectionText.transform.SetSiblingIndex(1);
            equipmentStatus.transform.SetSiblingIndex(2);
            equipmentDropdown.transform.SetSiblingIndex(3);
            SetRect((RectTransform)exploreButton.transform,
                new Vector2(0.18f, 0.17f), new Vector2(0.82f, 0.30f));
            exploreButton.transform.SetAsLastSibling();
            GameObject legacyGear = FindInScene(scene, "GearToggle");
            if (legacyGear != null) legacyGear.SetActive(false);
            GameObject residentialArea = FindInScene(scene, "ResidentialAreaButton") ??
                                         FindInScene(scene, "HardwareStoreButton");
            if (residentialArea != null)
            {
                residentialArea.name = "ResidentialAreaButton";
                Text label = residentialArea.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "주택가 탐사";
            }

            Button[] locationButtons =
            {
                FindInScene(scene, "ConvenienceStoreButton").GetComponent<Button>(),
                FindInScene(scene, "HospitalButton").GetComponent<Button>(),
                residentialArea.GetComponent<Button>(),
                EnsureHotspotButton(FindInScene(scene, "PoliceStationHotspot")),
                EnsureHotspotButton(FindInScene(scene, "CommunicationsStationHotspot"))
            };
            CreateCompletionOverlay(canvasObject.transform, out GameObject completionOverlay, out Text completionText);
            CreateDailyNotice(canvasObject.transform, out GameObject dailyNotice, out Text dailyNoticeText);

            ExplorationSceneController controller = controllerObject.GetComponent<ExplorationSceneController>();
            ExplorationEquipmentSelector equipmentSelector =
                controllerObject.GetComponent<ExplorationEquipmentSelector>();
            if (equipmentSelector == null)
                equipmentSelector = controllerObject.AddComponent<ExplorationEquipmentSelector>();
            SerializedObject equipmentSerialized = new(equipmentSelector);
            equipmentSerialized.FindProperty("weapon1Sprite").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(Weapon1IconPath);
            equipmentSerialized.FindProperty("weapon2Sprite").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(Weapon2IconPath);
            equipmentSerialized.FindProperty("weapon1Texture").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon1IconPath);
            equipmentSerialized.FindProperty("weapon2Texture").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon2IconPath);
            equipmentSerialized.ApplyModifiedPropertiesWithoutUndo();
            equipmentSelector.Configure(equipmentDropdown, equipmentStatus);
            SerializedObject serialized = new(controller);
            SerializedProperty buttonsProperty = serialized.FindProperty("locationButtons");
            buttonsProperty.arraySize = locationButtons.Length;
            for (int i = 0; i < locationButtons.Length; i++)
                buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = locationButtons[i];
            serialized.FindProperty("selectedLocationText").objectReferenceValue = selectionText;
            serialized.FindProperty("exploreButton").objectReferenceValue = exploreButton;
            serialized.FindProperty("exploreButtonFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Font>(ExplorationFontPath);
            serialized.FindProperty("equipmentSelector").objectReferenceValue = equipmentSelector;
            serialized.FindProperty("completionOverlay").objectReferenceValue = completionOverlay;
            serialized.FindProperty("completionText").objectReferenceValue = completionText;
            serialized.FindProperty("dailyNotice").objectReferenceValue = dailyNotice;
            serialized.FindProperty("dailyNoticeText").objectReferenceValue = dailyNoticeText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ExplorationLocationData[] locationData = EnsureLocationData();
            SerializedObject locationSerialized = new(controller);
            SerializedProperty locationDataProperty = locationSerialized.FindProperty("locationData");
            locationDataProperty.arraySize = locationData.Length;
            for (int i = 0; i < locationData.Length; i++)
                locationDataProperty.GetArrayElementAtIndex(i).objectReferenceValue = locationData[i];
            locationSerialized.ApplyModifiedPropertiesWithoutUndo();
            AssignShelterLocations(locationData);
            ApplyExplorationFont(canvasObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Yesterday Map: Exploration responsive layout and selection UI repaired.");

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static Button EnsureHotspotButton(GameObject hotspot)
        {
            Button button = hotspot.GetComponent<Button>();
            if (button == null) button = hotspot.AddComponent<Button>();
            button.targetGraphic = hotspot.GetComponent<Image>();
            return button;
        }

        private static void CreateCompletionOverlay(Transform canvas, out GameObject overlay, out Text message)
        {
            Transform existing = canvas.Find("ExplorationCompletionOverlay");
            overlay = existing != null ? existing.gameObject : new GameObject(
                "ExplorationCompletionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(canvas, false);
            SetRect((RectTransform)overlay.transform, Vector2.zero, Vector2.one);
            Image background = overlay.GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExplorationResultBackgroundPath);
            background.color = Color.white;
            background.preserveAspect = false;
            Transform shadeTransform = overlay.transform.Find("ResultImageShade");
            GameObject shadeObject = shadeTransform != null ? shadeTransform.gameObject : new GameObject(
                "ResultImageShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadeObject.transform.SetParent(overlay.transform, false);
            SetRect((RectTransform)shadeObject.transform, Vector2.zero, Vector2.one);
            Image shade = shadeObject.GetComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.6f);
            shade.raycastTarget = false;
            shadeObject.transform.SetAsFirstSibling();
            message = EnsureTextPreservingExistingSize(overlay.transform, "CompletionText", string.Empty, 34,
                new Vector2(0.16f, 0.4f), new Vector2(0.84f, 0.6f));
            message.color = Color.white;
            Text diaryHint = EnsureTextPreservingExistingSize(overlay.transform, "DiaryResultHint",
                "일기장에서 결과를 확인해주세요.", 20,
                new Vector2(0.3f, 0.26f), new Vector2(0.7f, 0.33f));
            diaryHint.color = new Color(1f, 1f, 1f, 0.82f);
            Text hint = EnsureTextPreservingExistingSize(overlay.transform, "DismissHint", "좌클릭 또는 Space", 20,
                new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.25f));
            hint.color = new Color(1f, 1f, 1f, 0.62f);
            overlay.SetActive(false);
        }

        private static void CreateDailyNotice(Transform canvas, out GameObject notice, out Text message)
        {
            Transform existing = canvas.Find("DailyExplorationNotice");
            notice = existing != null ? existing.gameObject : new GameObject(
                "DailyExplorationNotice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            notice.transform.SetParent(canvas, false);
            SetRect((RectTransform)notice.transform, new Vector2(0.27f, 0.43f), new Vector2(0.73f, 0.57f));
            Image background = notice.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.82f);
            background.raycastTarget = false;
            message = EnsureText(notice.transform, "NoticeText", "오늘은 이미 갔다왔다", 32,
                Vector2.zero, Vector2.one);
            message.color = Color.white;
            notice.SetActive(false);
        }

        private static ExplorationLocationData[] EnsureLocationData() => new[]
        {
            EnsureLocation("ConvenienceStore", "편의점", 0.20f, 1, "식량, 식수",
                new[]
                {
                    Reward(ResourceType.Food, 0, 2, 50f, Amounts(10f, 80f, 10f), Amounts(0f, 70f, 30f)),
                    Reward(ResourceType.Water, 0, 2, 50f, Amounts(10f, 80f, 10f), Amounts(0f, 70f, 30f))
                }),
            EnsureLocation("Hospital", "병원", 0.42f, 3, "약품, 식수",
                new[]
                {
                    Reward(ResourceType.Medicine, 1, 1, 40f, Amounts(0f, 100f), Amounts(0f, 100f)),
                    Reward(ResourceType.Water, 0, 2, 60f, Amounts(30f, 60f, 10f), Amounts(10f, 70f, 20f))
                }),
            EnsureLocation("ResidentialArea", "주택가", 0.34f, 2, "식량, 식수, 연료",
                new[]
                {
                    Reward(ResourceType.Food, 0, 2, 40f, Amounts(20f, 70f, 10f), Amounts(0f, 80f, 20f)),
                    Reward(ResourceType.Water, 0, 2, 40f, Amounts(20f, 70f, 10f), Amounts(0f, 80f, 20f)),
                    Reward(ResourceType.Fuel, 0, 1, 20f, Amounts(50f, 50f), Amounts(50f, 50f))
                }),
            EnsureLocation("PoliceStation", "경찰서", 0.52f, 4, "약품, 수리키트, 연료",
                new[]
                {
                    Reward(ResourceType.Medicine, 0, 1, 25f, Amounts(30f, 70f), Amounts(0f, 100f)),
                    Reward(ResourceType.Parts, 1, 3, 25f, Amounts(0f, 50f, 25f, 25f), Amounts(0f, 50f, 25f, 25f)),
                    Reward(ResourceType.Fuel, 0, 2, 50f, Amounts(20f, 60f, 20f), Amounts(0f, 60f, 40f))
                }),
            EnsureLocation("CommunicationsStation", "통신소", 0.46f, 2, "수리키트, 연료",
                new[]
                {
                    Reward(ResourceType.Parts, 0, 5, 60f, Amounts(1f, 1f, 1f, 1f, 1f, 1f), Amounts(0f, 1f, 1f, 1f, 1f, 1f)),
                    Reward(ResourceType.Fuel, 0, 2, 40f, Amounts(20f, 60f, 20f), Amounts(0f, 60f, 40f))
                })
        };

        private static ExplorationLocationData EnsureLocation(string assetName, string displayName, float danger, int dangerStars,
            string summary, ExplorationLocationData.RewardEntry[] rewards)
        {
            string path = $"Assets/_Project/Data/{assetName}.asset";
            ExplorationLocationData data = AssetDatabase.LoadAssetAtPath<ExplorationLocationData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ExplorationLocationData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.Configure(displayName, danger, rewards, summary, dangerStars);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static ExplorationLocationData.RewardEntry Reward(ResourceType type, int min, int max,
            float selectionWeight, ExplorationLocationData.AmountChance[] normal,
            ExplorationLocationData.AmountChance[] equipped) => new()
        {
            type = type, minAmount = min, maxAmount = max, selectionWeight = selectionWeight,
            amountChances = normal, equippedAmountChances = equipped
        };

        private static ExplorationLocationData.AmountChance[] Amounts(params float[] weights)
        {
            ExplorationLocationData.AmountChance[] chances = new ExplorationLocationData.AmountChance[weights.Length];
            for (int amount = 0; amount < weights.Length; amount++)
                chances[amount] = new ExplorationLocationData.AmountChance { amount = amount, weight = weights[amount] };
            return chances;
        }

        private static void AssignShelterLocations(ExplorationLocationData[] locations)
        {
            const string shelterPath = "Assets/_Project/Scenes/Shelter.unity";
            Scene shelter = SceneManager.GetSceneByPath(shelterPath);
            bool opened = !shelter.IsValid() || !shelter.isLoaded;
            if (opened) shelter = EditorSceneManager.OpenScene(shelterPath, OpenSceneMode.Additive);
            ExplorationManager manager = FindInScene(shelter, "ShelterManagers")?.GetComponent<ExplorationManager>();
            if (manager != null)
            {
                SerializedObject serialized = new(manager);
                SerializedProperty property = serialized.FindProperty("locations");
                property.arraySize = locations.Length;
                for (int i = 0; i < locations.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = locations[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(shelter);
                EditorSceneManager.SaveScene(shelter);
            }
            if (opened) EditorSceneManager.CloseScene(shelter, true);
        }

        private static Text EnsureText(Transform parent, string name, string value, int size,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existing = parent.Find(name);
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text == null)
            {
                GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                child.transform.SetParent(parent, false);
                text = child.GetComponent<Text>();
            }
            SetRect(text.rectTransform, anchorMin, anchorMax);
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private static Text EnsureTextPreservingExistingSize(Transform parent, string name, string value, int defaultSize,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Text existing = parent.Find(name)?.GetComponent<Text>();
            int fontSize = existing != null ? existing.fontSize : defaultSize;
            Text text = EnsureText(parent, name, value, defaultSize, anchorMin, anchorMax);
            text.fontSize = fontSize;
            return text;
        }

        private static RectTransform EnsureNotePanel(RectTransform mapContent, Scene scene)
        {
            RectTransform note = FindInScene(scene, "LocationTitlePanel")?.GetComponent<RectTransform>();
            if (note != null) return note;

            GameObject noteObject = new("LocationTitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            note = noteObject.GetComponent<RectTransform>();
            note.SetParent(mapContent, false);
            note.anchorMin = new Vector2(0.76f, 0.22f);
            note.anchorMax = new Vector2(0.98f, 0.7f);
            note.offsetMin = Vector2.zero;
            note.offsetMax = Vector2.zero;
            Image background = noteObject.GetComponent<Image>();
            background.color = new Color(0.88f, 0.79f, 0.61f, 0.94f);
            background.raycastTarget = false;
            return note;
        }

        private static Button EnsureButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existing = parent.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                child.transform.SetParent(parent, false);
                button = child.GetComponent<Button>();
                button.targetGraphic = child.GetComponent<Image>();
            }
            SetRect((RectTransform)button.transform, anchorMin, anchorMax);
            button.targetGraphic.color = new Color(0.32f, 0.17f, 0.08f, 0.95f);
            Text text = EnsureText(button.transform, "Label", label, 40, Vector2.zero, Vector2.one);
            text.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Dropdown EnsureDropdown(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Transform existing = parent.Find(name);
            Dropdown dropdown = existing != null ? existing.GetComponent<Dropdown>() : null;
            if (dropdown == null)
            {
                GameObject child = DefaultControls.CreateDropdown(new DefaultControls.Resources());
                child.name = name;
                child.transform.SetParent(parent, false);
                dropdown = child.GetComponent<Dropdown>();
            }

            SetRect((RectTransform)dropdown.transform, anchorMin, anchorMax);
            Image image = dropdown.GetComponent<Image>();
            Color darkBrown = new Color(0.24f, 0.11f, 0.045f, 0.98f);
            Color brown = new Color(0.34f, 0.17f, 0.07f, 0.98f);
            Color lightBrown = new Color(0.48f, 0.27f, 0.12f, 1f);
            Color cream = new Color(0.95f, 0.88f, 0.72f, 1f);
            if (image != null) image.color = brown;

            SetImageColor(dropdown.transform.Find("Arrow"), cream);
            SetImageColor(dropdown.transform.Find("Template"), darkBrown);
            SetImageColor(dropdown.transform.Find("Template/Viewport"), darkBrown);
            SetImageColor(dropdown.transform.Find("Template/Viewport/Content/Item/Item Background"), brown);
            SetImageColor(dropdown.transform.Find("Template/Viewport/Content/Item/Item Checkmark"), cream);
            Transform itemTemplate = dropdown.transform.Find("Template/Viewport/Content/Item");
            if (itemTemplate != null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(itemTemplate.gameObject);
                RectTransform itemRect = itemTemplate as RectTransform;
                if (itemRect != null)
                    itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, 32f);
                Text itemLabel = itemTemplate.Find("Item Label")?.GetComponent<Text>();
                if (itemLabel != null)
                {
                    itemLabel.gameObject.SetActive(true);
                    itemLabel.enabled = true;
                    itemLabel.fontSize = 16;
                    itemLabel.alignment = TextAnchor.MiddleLeft;
                    itemLabel.color = cream;
                    itemLabel.verticalOverflow = VerticalWrapMode.Overflow;
                    RectTransform labelRect = itemLabel.rectTransform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(24f, 0f);
                    labelRect.offsetMax = new Vector2(-10f, 0f);
                    itemLabel.transform.SetAsLastSibling();
                }
            }
            SetImageColor(dropdown.transform.Find("Template/Scrollbar"), darkBrown);
            SetImageColor(dropdown.transform.Find("Template/Scrollbar/Sliding Area/Handle"), lightBrown);
            foreach (Text text in dropdown.GetComponentsInChildren<Text>(true))
            {
                text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 18;
                text.color = cream;
            }
            return dropdown;
        }

        private static void SetImageColor(Transform target, Color color)
        {
            if (target != null && target.TryGetComponent(out Image image))
                image.color = color;
        }

        private static void ApplyExplorationFont(GameObject canvasObject)
        {
            Font explorationFont = AssetDatabase.LoadAssetAtPath<Font>(ExplorationFontPath);
            if (explorationFont == null)
            {
                Debug.LogWarning($"탐사 UI 폰트를 찾지 못했습니다: {ExplorationFontPath}");
                return;
            }

            YesterdayMap.UI.KoreanFontApplicator automaticApplicator =
                canvasObject.GetComponent<YesterdayMap.UI.KoreanFontApplicator>();
            if (automaticApplicator != null)
                Object.DestroyImmediate(automaticApplicator);

            foreach (Text text in canvasObject.GetComponentsInChildren<Text>(true))
            {
                if (text.GetComponentInParent<Dropdown>(true) != null) continue;
                text.font = explorationFont;
                EditorUtility.SetDirty(text);
            }
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindRecursively(root.transform, objectName);
                if (match != null) return match.gameObject;
            }
            return null;
        }

        private static Transform FindRecursively(Transform current, string objectName)
        {
            if (current.name == objectName) return current;
            foreach (Transform child in current)
            {
                Transform match = FindRecursively(child, objectName);
                if (match != null) return match;
            }
            return null;
        }
    }
}
#endif
