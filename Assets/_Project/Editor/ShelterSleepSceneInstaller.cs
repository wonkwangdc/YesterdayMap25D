using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Shelter;
using YesterdayMap.UI;

namespace YesterdayMap.EditorTools
{
    // Installs the bed sleep confirmation popup into the existing Shelter scene.
    public static class ShelterSleepSceneInstaller
    {
        private const string ShelterScenePath = "Assets/_Project/Scenes/Shelter.unity";
        private const string PanelName = "SleepConfirmPanel";
        private const string RestBackgroundPath = "Assets/_Project/Scenes/UI/rest.png";
        private const string RestConfirmPath = "Assets/_Project/Scenes/UI/resty.png";
        private const string RestCancelPath = "Assets/_Project/Scenes/UI/restn.png";

        [MenuItem("Yesterday Map/Install Shelter Sleep UI")]
        public static void Install()
        {
            if (!File.Exists(ShelterScenePath))
            {
                Debug.LogError($"Shelter scene not found: {ShelterScenePath}");
                return;
            }

            if (Application.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before installing Shelter sleep UI.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ShelterScenePath)
            {
                EditorSceneManager.OpenScene(ShelterScenePath);
            }

            Canvas canvas = FindShelterCanvas();
            if (canvas == null)
            {
                Debug.LogError("Shelter scene has no Canvas.");
                return;
            }

            SleepConfirmationUI confirmation = CreateOrUpdatePanel(canvas.transform);
            LinkBed(confirmation);

            EditorUtility.SetDirty(confirmation);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Shelter sleep confirmation UI installed.");
        }

        private static SleepConfirmationUI CreateOrUpdatePanel(Transform canvas)
        {
            GameObject root = null;
            foreach (Transform child in canvas)
            {
                if (child.name == PanelName)
                {
                    root = child.gameObject;
                    break;
                }
            }
            if (root == null)
            {
                root = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.transform.SetParent(canvas, false);
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, Vector2.zero, Vector2.one);
            Image dim = root.GetComponent<Image>();
            dim.color = new Color(0.015f, 0.012f, 0.01f, 0.72f);
            dim.raycastTarget = true;

            Image box = EnsureImage(root.transform, "SleepConfirmBox", new Vector2(0.34f, 0.36f), new Vector2(0.66f, 0.62f));
            box.color = new Color(0.13f, 0.095f, 0.06f, 0.97f);

            Text title = EnsureText(root.transform, "SleepConfirmTitle", new Vector2(0.37f, 0.535f), new Vector2(0.63f, 0.595f), 30, TextAnchor.MiddleCenter);
            title.text = "수면";
            title.color = new Color(0.95f, 0.82f, 0.58f, 1f);

            Text message = EnsureText(root.transform, "SleepConfirmMessage", new Vector2(0.37f, 0.455f), new Vector2(0.63f, 0.525f), 26, TextAnchor.MiddleCenter);
            message.text = "잠을 자시겠습니까?";
            message.color = new Color(0.94f, 0.90f, 0.80f, 1f);

            Button confirm = EnsureButton(root.transform, "SleepConfirmButton", new Vector2(0.375f, 0.385f), new Vector2(0.49f, 0.445f), out Text confirmText);
            confirmText.text = "확인";

            Button cancel = EnsureButton(root.transform, "SleepCancelButton", new Vector2(0.51f, 0.385f), new Vector2(0.625f, 0.445f), out Text cancelText);
            cancelText.text = "취소";

            SleepConfirmationUI ui = root.GetComponent<SleepConfirmationUI>();
            if (ui == null)
            {
                ui = root.AddComponent<SleepConfirmationUI>();
            }

            box.sprite = ImportCroppedSprite(
                RestBackgroundPath,
                "RestBackground",
                new Rect(199f, 258f, 1133f, 628f));
            box.color = Color.white;
            box.preserveAspect = true;
            confirm.image.sprite = ImportCroppedSprite(
                RestConfirmPath,
                "RestConfirm",
                new Rect(200f, 325f, 1136f, 428f));
            confirm.image.color = Color.white;
            confirm.image.preserveAspect = true;
            cancel.image.sprite = ImportCroppedSprite(
                RestCancelPath,
                "RestCancel",
                new Rect(226f, 306f, 1083f, 419f));
            cancel.image.color = Color.white;
            cancel.image.preserveAspect = true;

            ui.Configure(root, message, confirm, cancel);
            root.SetActive(false);
            return ui;
        }

        private static void LinkBed(SleepConfirmationUI confirmation)
        {
            GameObject bedTarget = FindBedTarget();
            BedObject bed = bedTarget != null
                ? bedTarget.GetComponent<BedObject>()
                : Object.FindFirstObjectByType<BedObject>(FindObjectsInactive.Include);

            if (bed == null)
            {
                bed = bedTarget != null ? bedTarget.AddComponent<BedObject>() : null;
            }

            if (bed == null)
            {
                Debug.LogWarning("Bed target not found; sleep confirmation UI will not be linked.");
                return;
            }

            SceneFader fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
            CharacterStats stats = Object.FindFirstObjectByType<CharacterStats>(FindObjectsInactive.Include);
            DayCycleManager dayCycle = Object.FindFirstObjectByType<DayCycleManager>(FindObjectsInactive.Include);
            UIManager uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            bed.Configure(stats, dayCycle);
            bed.ConfigureBase(uiManager, "[E] 잠자기");

            SerializedObject serializedBed = new SerializedObject(bed);
            serializedBed.FindProperty("stats").objectReferenceValue = stats;
            serializedBed.FindProperty("dayCycle").objectReferenceValue = dayCycle;
            serializedBed.FindProperty("sleepConfirmation").objectReferenceValue = confirmation;
            serializedBed.FindProperty("sceneFader").objectReferenceValue = fader;
            serializedBed.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bed);
        }

        private static GameObject FindBedTarget()
        {
            GameObject exact = GameObject.Find("BunkerFurniture_IronBed");
            if (exact != null)
            {
                return exact;
            }

            foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.name.Contains("IronBed"))
                {
                    return candidate;
                }
            }

            return GameObject.Find("Bed");
        }

        private static Canvas FindShelterCanvas()
        {
            Canvas fallback = null;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                fallback ??= canvas;
                if (canvas.gameObject.name == "ShelterHUD")
                {
                    return canvas;
                }
            }

            return fallback;
        }

        private static Sprite ImportCroppedSprite(
            string path,
            string spriteName,
            Rect rect)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            bool needsImport = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Multiple ||
                               importer.spritesheet.Length != 1 ||
                               importer.spritesheet[0].name != spriteName ||
                               importer.spritesheet[0].rect != rect;
            if (needsImport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritesheet = new[]
                {
                    new SpriteMetaData
                    {
                        name = spriteName,
                        rect = rect,
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    }
                };
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
        }

        private static Image EnsureImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject child = EnsureChild(parent, name, typeof(Image));
            Image image = child.GetComponent<Image>();
            image.raycastTarget = false;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return image;
        }

        private static Text EnsureText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor alignment)
        {
            GameObject child = EnsureChild(parent, name, typeof(Text));
            Text text = child.GetComponent<Text>();
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.fontSize = size;
            text.alignment = alignment;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return text;
        }

        private static Button EnsureButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out Text label)
        {
            GameObject child = EnsureChild(parent, name, typeof(Image), typeof(Button));
            Image image = child.GetComponent<Image>();
            image.color = new Color(0.33f, 0.47f, 0.41f, 0.98f);
            Button button = child.GetComponent<Button>();
            button.targetGraphic = image;
            Stretch(child.GetComponent<RectTransform>(), anchorMin, anchorMax);

            label = EnsureText(child.transform, "Label", Vector2.zero, Vector2.one, 22, TextAnchor.MiddleCenter);
            label.color = new Color(0.96f, 0.92f, 0.82f, 1f);
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

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
