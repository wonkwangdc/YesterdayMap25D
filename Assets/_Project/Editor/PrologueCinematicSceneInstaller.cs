#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Core;
using YesterdayMap.UI;

namespace YesterdayMap.EditorTools
{
    public static class PrologueCinematicSceneInstaller
    {
        private const string PrologueScenePath =
            "Assets/_Project/Scenes/Prologue.unity";
        private const string RootName = "PrologueCinematicRoot";
        private static readonly string[] FramePaths =
        {
            PrologueArtworkImporter.ArtworkFolder + "prologue_01.png",
            PrologueArtworkImporter.ArtworkFolder + "prologue_02.png",
            PrologueArtworkImporter.ArtworkFolder + "prologue_03.png",
            PrologueArtworkImporter.ArtworkFolder + "prologue_04.png"
        };

        [MenuItem("Yesterday Map/Install Prologue Cinematic")]
        public static void Install()
        {
            Scene originalScene = EditorSceneManager.GetActiveScene();
            string originalPath = originalScene.path;
            if (originalScene.isDirty)
            {
                Debug.LogError(
                    "현재 씬에 저장되지 않은 변경이 있어 프롤로그 연출 설치를 중단했습니다.");
                return;
            }

            ConfigureArtworkImporters();
            Scene prologueScene = originalPath == PrologueScenePath
                ? originalScene
                : EditorSceneManager.OpenScene(
                    PrologueScenePath,
                    OpenSceneMode.Single);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            PrologueController controller =
                Object.FindFirstObjectByType<PrologueController>();
            if (canvas == null || controller == null)
            {
                Debug.LogError(
                    "Prologue 씬에서 Canvas 또는 PrologueController를 찾을 수 없습니다.");
                RestoreOriginalScene(originalPath);
                return;
            }

            InstallIntoCanvas(canvas.transform, controller);
            EditorSceneManager.MarkSceneDirty(prologueScene);
            EditorSceneManager.SaveScene(prologueScene);
            AssetDatabase.SaveAssets();

            RestoreOriginalScene(originalPath);
            Debug.Log(
                "Yesterday Map: 프롤로그 사진 4장과 자막 연출을 설치했습니다.");
        }

        public static PrologueCinematicSequence InstallIntoCanvas(
            Transform canvas,
            PrologueController controller)
        {
            ConfigureArtworkImporters();

            Transform existing = canvas.Find(RootName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Image root = CreateImage(
                canvas,
                RootName,
                Vector2.zero,
                Vector2.one,
                Color.black);
            root.raycastTarget = true;
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

            Image artwork = CreateImage(
                root.transform,
                "Artwork",
                Vector2.zero,
                Vector2.one,
                Color.white);
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            AspectRatioFitter artworkFitter =
                artwork.gameObject.AddComponent<AspectRatioFitter>();
            artworkFitter.aspectMode =
                AspectRatioFitter.AspectMode.EnvelopeParent;
            artworkFitter.aspectRatio = 1672f / 941f;

            GameObject captionPanel = CreateImage(
                root.transform,
                "CaptionPanel",
                new Vector2(0.12f, 0.035f),
                new Vector2(0.88f, 0.205f),
                new Color(0.015f, 0.012f, 0.01f, 0.84f)).gameObject;
            Text captionText = CreateText(
                captionPanel.transform,
                "CaptionText",
                Vector2.zero,
                Vector2.one,
                30,
                TextAnchor.MiddleCenter);

            GameObject newsPanel = CreateImage(
                root.transform,
                "BreakingNewsPanel",
                new Vector2(0.055f, 0.055f),
                new Vector2(0.945f, 0.18f),
                new Color(0.08f, 0.015f, 0.012f, 0.92f)).gameObject;
            Text newsHeader = CreateText(
                newsPanel.transform,
                "Header",
                new Vector2(0.015f, 0.08f),
                new Vector2(0.18f, 0.92f),
                27,
                TextAnchor.MiddleCenter);
            newsHeader.text = "속보";
            newsHeader.color = new Color(0.98f, 0.82f, 0.6f, 1f);
            newsHeader.fontStyle = FontStyle.Bold;

            Text newsText = CreateText(
                newsPanel.transform,
                "Headline",
                new Vector2(0.19f, 0.08f),
                new Vector2(0.98f, 0.92f),
                30,
                TextAnchor.MiddleLeft);

            GameObject alertPanel = CreateImage(
                root.transform,
                "EmergencyAlertPanel",
                new Vector2(0.56f, 0.51f),
                new Vector2(0.94f, 0.84f),
                new Color(0.025f, 0.025f, 0.03f, 0.94f)).gameObject;
            Image alertHeader = CreateImage(
                alertPanel.transform,
                "EmergencyHeader",
                new Vector2(0f, 0.86f),
                Vector2.one,
                new Color(0.55f, 0.08f, 0.07f, 1f));
            alertHeader.raycastTarget = false;
            Text alertText = CreateText(
                alertPanel.transform,
                "EmergencyText",
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.86f),
                25,
                TextAnchor.MiddleLeft);

            Text hint = CreateText(
                root.transform,
                "AdvanceHint",
                new Vector2(0.76f, 0.94f),
                new Vector2(0.985f, 0.99f),
                17,
                TextAnchor.MiddleRight);
            hint.color = new Color(1f, 1f, 1f, 0.48f);

            Sprite[] frames = new Sprite[FramePaths.Length];
            for (int i = 0; i < FramePaths.Length; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(FramePaths[i]);
                if (frames[i] == null)
                {
                    throw new MissingReferenceException(
                        $"프롤로그 사진을 불러올 수 없습니다: {FramePaths[i]}");
                }
            }

            PrologueCinematicSequence sequence =
                root.gameObject.AddComponent<PrologueCinematicSequence>();
            sequence.Configure(
                group,
                artwork,
                captionPanel,
                captionText,
                newsPanel,
                newsText,
                alertPanel,
                alertText,
                hint,
                frames);
            controller.ConfigureCinematic(sequence);

            Transform sceneFade = canvas.Find("SceneFade");
            if (sceneFade != null) sceneFade.SetAsLastSibling();
            root.gameObject.SetActive(false);
            return sequence;
        }

        private static void ConfigureArtworkImporters()
        {
            AssetDatabase.Refresh();
            foreach (string path in FramePaths)
            {
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new MissingReferenceException(
                        $"프롤로그 사진 파일을 찾을 수 없습니다: {path}");

                bool changed =
                    importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    importer.mipmapEnabled ||
                    importer.npotScale != TextureImporterNPOTScale.None ||
                    importer.maxTextureSize < 2048;
                if (!changed) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression =
                    TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static Image CreateImage(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject owner = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            owner.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)owner.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = owner.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject owner = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            owner.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)owner.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);

            Text text = owner.GetComponent<Text>();
            text.font = EditorUIFont.Default;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.92f, 0.86f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;

            Outline outline = owner.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static void RestoreOriginalScene(string originalPath)
        {
            if (!string.IsNullOrWhiteSpace(originalPath) &&
                originalPath != PrologueScenePath)
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }
        }
    }
}
#endif
