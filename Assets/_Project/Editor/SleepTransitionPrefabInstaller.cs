#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.UI;

namespace YesterdayMap.Editor
{
    /// <summary>
    /// Creates the self-contained sleep transition prefab without editing Shelter.unity.
    /// </summary>
    public static class SleepTransitionPrefabInstaller
    {
        private const string ArtworkPath =
            "Assets/_Project/Resources/UI/Sleep/SleepTransition.png";
        private const string PrefabPath =
            "Assets/_Project/Resources/UI/Sleep/SleepTransition.prefab";
        private const string GaugeMaterialPath =
            "Assets/_Project/Materials/UI/ExplorationLoadingGauge.mat";
        private const float ArtworkAspectRatio = 1672f / 941f;

        private static readonly Rect GaugeRect =
            new(0.052f, 0.1276f, 0.451f, 0.048f);
        private static readonly Color TrackColor =
            new(0.15f, 0.14f, 0.125f, 1f);
        private static readonly Color FillColor =
            new(0.46f, 0.45f, 0.42f, 1f);

        [MenuItem("Yesterday Map/Create Sleep Transition Prefab")]
        public static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before creating the sleep transition prefab.");
                return;
            }

            PrepareArtworkImporter();
            Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
            Material gaugeMaterial = AssetDatabase.LoadAssetAtPath<Material>(GaugeMaterialPath);
            if (artwork == null || gaugeMaterial == null)
            {
                Debug.LogError("Sleep transition artwork or exploration gauge material is missing.");
                return;
            }

            GameObject root = new(
                "SleepTransition",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(SleepTransitionView));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32760;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1672f, 941f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject blockerObject = new(
                "SleepTransitionOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            blockerObject.transform.SetParent(root.transform, false);
            Stretch(blockerObject.GetComponent<RectTransform>());
            Image blocker = blockerObject.GetComponent<Image>();
            blocker.color = Color.black;
            blocker.raycastTarget = true;
            CanvasGroup blockerGroup = blockerObject.GetComponent<CanvasGroup>();
            blockerGroup.interactable = true;
            blockerGroup.blocksRaycasts = true;

            GameObject artworkFrame = new(
                "SleepArtworkFrame",
                typeof(RectTransform),
                typeof(AspectRatioFitter));
            artworkFrame.transform.SetParent(blockerObject.transform, false);
            Stretch(artworkFrame.GetComponent<RectTransform>());
            AspectRatioFitter aspectFitter = artworkFrame.GetComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = ArtworkAspectRatio;

            GameObject artworkObject = new(
                "SleepArtwork",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            artworkObject.transform.SetParent(artworkFrame.transform, false);
            Stretch(artworkObject.GetComponent<RectTransform>());
            RawImage artworkImage = artworkObject.GetComponent<RawImage>();
            artworkImage.texture = artwork;
            artworkImage.color = Color.white;
            artworkImage.raycastTarget = false;

            GameObject progressObject = new(
                "SleepProgressOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            progressObject.transform.SetParent(artworkFrame.transform, false);
            Stretch(progressObject.GetComponent<RectTransform>());
            RawImage progressOverlay = progressObject.GetComponent<RawImage>();
            progressOverlay.texture = artwork;
            progressOverlay.material = gaugeMaterial;
            progressOverlay.color = Color.white;
            progressOverlay.raycastTarget = false;

            root.GetComponent<SleepTransitionView>().Configure(
                artworkImage,
                progressOverlay,
                3f,
                GaugeRect,
                TrackColor,
                FillColor);
            root.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Yesterday Map: sleep transition prefab created without modifying Shelter.unity.");
        }

        private static void PrepareArtworkImporter()
        {
            if (AssetImporter.GetAtPath(ArtworkPath) is not TextureImporter importer)
                return;

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

            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
