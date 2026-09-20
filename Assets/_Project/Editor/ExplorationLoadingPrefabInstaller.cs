#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Exploration;

namespace YesterdayMap.Editor
{
    /// <summary>
    /// Creates one editable loading prefab and connects it to Exploration.
    /// Existing prefab and scene instances are preserved.
    /// </summary>
    public static class ExplorationLoadingPrefabInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/Exploration.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/ExplorationLoading.prefab";
        private const string GaugeMaterialPath =
            "Assets/_Project/Materials/UI/ExplorationLoadingGauge.mat";
        private const string GaugeShaderName =
            "YesterdayMap/UI/ExplorationLoadingGauge";
        private const float ArtworkAspectRatio = 1672f / 941f;

        private static readonly string[] LocationPaths =
        {
            "Assets/_Project/Data/ConvenienceStore.asset",
            "Assets/_Project/Data/Hospital.asset",
            "Assets/_Project/Data/ResidentialArea.asset",
            "Assets/_Project/Data/PoliceStation.asset",
            "Assets/_Project/Data/CommunicationsStation.asset"
        };

        private static readonly string[] ArtworkPaths =
        {
            "Assets/_Project/Resources/UI/ExplorationLoading/ExplorationLoading_ConvenienceStore.png",
            "Assets/_Project/Resources/UI/ExplorationLoading/ExplorationLoading_Hospital.png",
            "Assets/_Project/Resources/UI/ExplorationLoading/ExplorationLoading_ResidentialArea.png",
            "Assets/_Project/Resources/UI/ExplorationLoading/ExplorationLoading_PoliceStation.png",
            "Assets/_Project/Resources/UI/ExplorationLoading/ExplorationLoading_CommunicationsStation.png"
        };

        // Each artwork places its native baseline a fraction of a pixel apart.
        private static readonly Rect[] GaugeRects =
        {
            new(0.046f, 0.0927f, 0.442f, 0.048f),
            new(0.046f, 0.0928f, 0.442f, 0.048f),
            new(0.046f, 0.0934f, 0.442f, 0.048f),
            new(0.046f, 0.1007f, 0.442f, 0.048f),
            new(0.046f, 0.0936f, 0.442f, 0.048f)
        };

        private static readonly Color[] GaugeTrackColors =
        {
            new(0.178f, 0.157f, 0.132f, 1f),
            new(0.180f, 0.165f, 0.147f, 1f),
            new(0.171f, 0.155f, 0.132f, 1f),
            new(0.155f, 0.143f, 0.127f, 1f),
            new(0.181f, 0.164f, 0.145f, 1f)
        };

        private static readonly Color[] GaugeFillColors =
        {
            new(0.447f, 0.459f, 0.447f, 1f),
            new(0.427f, 0.400f, 0.369f, 1f),
            new(0.451f, 0.439f, 0.416f, 1f),
            new(0.447f, 0.427f, 0.400f, 1f),
            new(0.459f, 0.447f, 0.427f, 1f)
        };

        [MenuItem("Yesterday Map/Install Exploration Loading Prefab")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before installing the exploration loading prefab.");
                return;
            }

            GameObject prefab = GetOrCreatePrefab();
            if (prefab == null) return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            else if (scene.isDirty)
            {
                Debug.LogWarning(
                    "Exploration.unity has unsaved changes. Save or revert them before installing the loading prefab.");
                return;
            }

            ExplorationSceneController controller =
                FindInScene<ExplorationSceneController>(scene);
            Canvas canvas = FindInScene<Canvas>(scene);
            if (controller == null || canvas == null)
            {
                Debug.LogError("Exploration scene requires a controller and Canvas before loading UI installation.");
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            AttachToCanvas(canvas.transform, controller, prefab, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Yesterday Map: reusable ExplorationLoading prefab is connected to Exploration.unity.");
            if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
        }

        public static GameObject GetOrCreatePrefab()
        {
            Material gaugeMaterial = GetOrCreateGaugeMaterial();
            if (gaugeMaterial == null) return null;

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;

            ExplorationLocationData[] locations = LoadAssets<ExplorationLocationData>(LocationPaths);
            Texture2D[] artworks = LoadAssets<Texture2D>(ArtworkPaths);
            if (HasMissingReference(locations) || HasMissingReference(artworks))
            {
                Debug.LogError("Exploration loading prefab assets are incomplete. Check location data and artwork paths.");
                return null;
            }

            GameObject root = new(
                "ExplorationLoadingOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(ExplorationLoadingView));
            Stretch(root.GetComponent<RectTransform>());
            Image blocker = root.GetComponent<Image>();
            blocker.color = Color.black;
            blocker.raycastTarget = true;
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            GameObject artworkFrame = new(
                "LoadingArtworkFrame",
                typeof(RectTransform),
                typeof(AspectRatioFitter));
            artworkFrame.transform.SetParent(root.transform, false);
            Stretch(artworkFrame.GetComponent<RectTransform>());
            AspectRatioFitter aspectFitter = artworkFrame.GetComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = ArtworkAspectRatio;

            GameObject artworkObject = new(
                "LoadingArtwork",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            artworkObject.transform.SetParent(artworkFrame.transform, false);
            Stretch(artworkObject.GetComponent<RectTransform>());
            RawImage artworkImage = artworkObject.GetComponent<RawImage>();
            artworkImage.color = Color.white;
            artworkImage.raycastTarget = false;

            GameObject progressOverlayObject = new(
                "LoadingProgressOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            progressOverlayObject.transform.SetParent(artworkFrame.transform, false);
            Stretch(progressOverlayObject.GetComponent<RectTransform>());
            RawImage progressOverlay = progressOverlayObject.GetComponent<RawImage>();
            progressOverlay.color = Color.white;
            progressOverlay.material = gaugeMaterial;
            progressOverlay.raycastTarget = false;

            ExplorationLoadingView view = root.GetComponent<ExplorationLoadingView>();
            view.Configure(
                artworkImage,
                progressOverlay,
                locations,
                artworks,
                GaugeRects,
                GaugeTrackColors,
                GaugeFillColors,
                3f);
            root.SetActive(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static ExplorationLoadingView AttachToCanvas(
            Transform canvas,
            ExplorationSceneController controller)
        {
            Scene scene = canvas != null ? canvas.gameObject.scene : default;
            return AttachToCanvas(canvas, controller, GetOrCreatePrefab(), scene);
        }

        private static ExplorationLoadingView AttachToCanvas(
            Transform canvas,
            ExplorationSceneController controller,
            GameObject prefab,
            Scene scene)
        {
            if (canvas == null || controller == null || prefab == null) return null;

            ExplorationLoadingView view = FindInScene<ExplorationLoadingView>(scene);
            if (view == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "ExplorationLoadingOverlay";
                instance.transform.SetParent(canvas, false);
                view = instance.GetComponent<ExplorationLoadingView>();
            }

            controller.ConfigureLoading(view);
            EditorUtility.SetDirty(controller);
            return view;
        }

        private static T[] LoadAssets<T>(string[] paths) where T : Object
        {
            T[] assets = new T[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
            return assets;
        }

        private static bool HasMissingReference<T>(T[] assets) where T : Object
        {
            if (assets == null || assets.Length == 0) return true;
            foreach (T asset in assets)
            {
                if (asset == null) return true;
            }
            return false;
        }

        private static Material GetOrCreateGaugeMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GaugeMaterialPath);
            if (material != null) return material;

            Shader shader = Shader.Find(GaugeShaderName);
            if (shader == null)
            {
                Debug.LogError($"Exploration loading gauge shader was not found: {GaugeShaderName}");
                return null;
            }

            string folder = System.IO.Path.GetDirectoryName(GaugeMaterialPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                string name = System.IO.Path.GetFileName(folder);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                    AssetDatabase.CreateFolder(parent, name);
            }

            material = new Material(shader) { name = "ExplorationLoadingGauge" };
            AssetDatabase.CreateAsset(material, GaugeMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
