using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YesterdayMap.Events;
using YesterdayMap.Shelter;

namespace YesterdayMap.EditorTools
{
    [InitializeOnLoad]
    public static class ShelterRadioModelInstaller
    {
        private const string ShelterScenePath = "Assets/_Project/Scenes/Shelter.unity";
        private const string RadioPrefabPath = "Assets/_Project/Prefabs/Facilities/Radio.prefab";
        private const string ModelPath =
            "Assets/_Project/Resources/Models/Radio/VintageFieldRadio.fbx";
        private const string AlbedoPath =
            "Assets/_Project/Resources/Models/Radio/VintageFieldRadio_Albedo.png";
        private const string NormalPath =
            "Assets/_Project/Resources/Models/Radio/VintageFieldRadio_Normal.png";
        private const string MetallicPath =
            "Assets/_Project/Resources/Models/Radio/VintageFieldRadio_Metallic.png";
        private const string MaterialPath =
            "Assets/_Project/Materials/Environment/VintageFieldRadio.mat";
        private const string VisualName = "VintageFieldRadioVisual";
        private const string IndicatorName = "RadioPowerGlow";
        private const string DialGlowName = "RadioDialGlow";
        private const string LedGlowName = "RadioPowerLedGlow";
        private const float VisualWidth = 0.9f;

        static ShelterRadioModelInstaller()
        {
            EditorApplication.delayCall += TryAutomaticInstall;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Yesterday Map/Install Shelter Radio Model")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before installing the shelter radio model.");
                return;
            }

            Material material = CreateOrUpdateMaterial();
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (material == null || modelPrefab == null)
            {
                Debug.LogError("Shelter radio assets are missing; placement was not changed.");
                return;
            }

            InstallIntoRadioPrefab(modelPrefab, material);
            InstallIntoShelterScene(modelPrefab, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Vintage field radio was placed and saved in the prefab and Shelter scene.");
        }

        private static void TryAutomaticInstall()
        {
            if (IsAlreadyInstalled())
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Install();
        }

        private static bool IsAlreadyInstalled()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            string radioPrefabGuid = AssetDatabase.AssetPathToGUID(RadioPrefabPath);
            if (material == null ||
                material.shader == null ||
                material.shader.name != "Standard" ||
                material.mainTexture != albedo ||
                material.GetTexture("_EmissionMap") != null ||
                !File.Exists(ShelterScenePath) ||
                !File.Exists(RadioPrefabPath))
            {
                return false;
            }

            string sceneText = File.ReadAllText(ShelterScenePath);
            string prefabText = File.ReadAllText(RadioPrefabPath);
            return sceneText.Contains(VisualName) &&
                   !sceneText.Contains(IndicatorName) &&
                   !sceneText.Contains(DialGlowName) &&
                   !sceneText.Contains(LedGlowName) &&
                   !sceneText.Contains($"guid: {radioPrefabGuid}") &&
                   prefabText.Contains(VisualName) &&
                   !prefabText.Contains(IndicatorName) &&
                   !prefabText.Contains(DialGlowName) &&
                   !prefabText.Contains(LedGlowName);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryAutomaticInstall;
            }
        }

        private static Material CreateOrUpdateMaterial()
        {
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath);
            // This project currently renders with the Built-in pipeline. A URP/Lit
            // material turns magenta here even though the URP package is installed.
            Shader shader = Shader.Find("Standard");
            if (shader == null || albedo == null)
            {
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "VintageFieldRadio"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetTexture("_MainTex", albedo);
            material.SetColor("_Color", Color.white);
            material.SetTexture("_EmissionMap", null);
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.85f);
                material.EnableKeyword("_NORMALMAP");
            }

            if (metallic != null)
            {
                material.SetTexture("_MetallicGlossMap", metallic);
                material.SetFloat("_Metallic", 1f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            material.SetFloat("_Smoothness", 0.24f);
            material.SetFloat("_Glossiness", 0.24f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void InstallIntoRadioPrefab(
            GameObject modelPrefab,
            Material material)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RadioPrefabPath);
            try
            {
                Transform visual = root.transform.Find(VisualName);
                if (visual == null)
                {
                    visual = CreateVisual(modelPrefab, root.transform).transform;
                }

                ConfigureVisual(visual.gameObject, material, null);
                RadioObject radio = root.GetComponent<RadioObject>();
                if (radio == null)
                {
                    radio = root.AddComponent<RadioObject>();
                }
                RemovePowerEffects(root);

                SerializedObject radioSettings = new SerializedObject(radio);
                radioSettings.FindProperty("focusDistance").floatValue = 1.35f;
                radioSettings.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, RadioPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void InstallIntoShelterScene(
            GameObject modelPrefab,
            Material material)
        {
            Scene scene = FindLoadedScene(ShelterScenePath);
            bool openedAdditively = !scene.IsValid();
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ShelterScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject desk = FindInScene(scene, "BunkerFurniture_Desk");
                if (desk == null)
                {
                    Debug.LogError("BunkerFurniture_Desk was not found in the Shelter scene.");
                    return;
                }

                DeskStoryObject legacy = desk.GetComponent<DeskStoryObject>();
                ShelterEventDialogueController dialogue = legacy != null
                    ? GetDialogueReference(legacy)
                    : Object.FindObjectsByType<ShelterEventDialogueController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None)
                        .FirstOrDefault(candidate => candidate.gameObject.scene == scene);

                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy);
                }

                Transform visual = desk.transform.Find(VisualName);
                if (visual == null)
                {
                    Transform stale = desk.transform.Find("__VintageFieldRadioVisual");
                    if (stale != null)
                    {
                        Object.DestroyImmediate(stale.gameObject);
                    }
                    visual = CreateVisual(modelPrefab, desk.transform).transform;
                }

                Bounds? deskBounds = TryGetBoundsExcluding(desk, visual);
                ConfigureVisual(visual.gameObject, material, deskBounds);

                // Interaction now belongs to the physical radio, not to the whole desk.
                RadioObject oldDeskRadio = desk.GetComponent<RadioObject>();
                RadioObject radio = visual.GetComponent<RadioObject>();
                if (radio == null)
                {
                    radio = visual.gameObject.AddComponent<RadioObject>();
                }

                SerializedObject serializedRadio = new SerializedObject(radio);
                serializedRadio.FindProperty("eventDialogue").objectReferenceValue = dialogue;
                serializedRadio.ApplyModifiedPropertiesWithoutUndo();
                RemovePowerEffects(visual.gameObject);

                SerializedObject radioSettings = new SerializedObject(radio);
                radioSettings.FindProperty("focusDistance").floatValue = 1.35f;
                radioSettings.ApplyModifiedPropertiesWithoutUndo();

                if (oldDeskRadio != null && oldDeskRadio != radio)
                {
                    Object.DestroyImmediate(oldDeskRadio);
                }

                RemoveDuplicateRadioPrefab(scene, visual.gameObject);

                EditorUtility.SetDirty(desk);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedAdditively && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GameObject CreateVisual(GameObject modelPrefab, Transform parent)
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(modelPrefab, parent) as GameObject;
            if (visual == null)
            {
                visual = Object.Instantiate(modelPrefab, parent);
            }
            visual.name = VisualName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            return visual;
        }

        private static void ConfigureVisual(
            GameObject visual,
            Material material,
            Bounds? supportingSurface)
        {
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    materials = new Material[1];
                }
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }
                renderer.sharedMaterials = materials;
            }

            if (!TryGetBounds(visual, out Bounds modelBounds))
            {
                return;
            }

            float horizontalSize = Mathf.Max(modelBounds.size.x, modelBounds.size.z);
            float scale = VisualWidth / Mathf.Max(0.001f, horizontalSize);
            visual.transform.localScale *= scale;

            if (!TryGetBounds(visual, out modelBounds))
            {
                return;
            }

            if (supportingSurface.HasValue)
            {
                Bounds deskBounds = supportingSurface.Value;
                Vector3 targetBase = new Vector3(
                    deskBounds.center.x,
                    deskBounds.max.y + 0.015f,
                    deskBounds.center.z);
                Vector3 modelBase = new Vector3(
                    modelBounds.center.x,
                    modelBounds.min.y,
                    modelBounds.center.z);
                visual.transform.position += targetBase - modelBase;
            }
            else
            {
                Vector3 modelBase = new Vector3(
                    modelBounds.center.x,
                    modelBounds.min.y,
                    modelBounds.center.z);
                visual.transform.position -= modelBase;
            }

            ConfigureCollider(visual);
            EditorUtility.SetDirty(visual);
        }

        private static void ConfigureCollider(GameObject visual)
        {
            if (!TryGetBounds(visual, out Bounds worldBounds))
            {
                return;
            }

            BoxCollider collider = visual.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = visual.AddComponent<BoxCollider>();
            }

            collider.center = visual.transform.InverseTransformPoint(worldBounds.center);
            Vector3 scale = visual.transform.lossyScale;
            collider.size = new Vector3(
                worldBounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                worldBounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                worldBounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            collider.isTrigger = false;
        }

        private static void RemovePowerEffects(GameObject radioRoot)
        {
            string[] effectNames =
            {
                IndicatorName,
                DialGlowName,
                LedGlowName
            };

            foreach (string effectName in effectNames)
            {
                Transform effect = radioRoot.transform.Find(effectName);
                if (effect != null)
                {
                    Object.DestroyImmediate(effect.gameObject);
                }
            }
        }

        private static void RemoveDuplicateRadioPrefab(Scene scene, GameObject deskRadio)
        {
            // Collect first, then destroy. Destroying while walking a prefab's
            // Transform hierarchy can otherwise leave missing Transform entries
            // in the enumerator when there is more than one stale instance.
            GameObject[] duplicates = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(candidate =>
                    candidate != null && candidate.gameObject != deskRadio)
                .Select(candidate =>
                    PrefabUtility.GetNearestPrefabInstanceRoot(candidate.gameObject))
                .Where(instanceRoot =>
                    instanceRoot != null &&
                    instanceRoot.name == "Radio" &&
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot) ==
                    RadioPrefabPath)
                .Distinct()
                .ToArray();

            foreach (GameObject duplicate in duplicates)
            {
                Object.DestroyImmediate(duplicate);
            }
        }

        private static Bounds? TryGetBoundsExcluding(GameObject root, Transform excluded)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !renderer.transform.IsChildOf(excluded))
                .ToArray();
            if (renderers.Length == 0)
            {
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return true;
        }

        private static ShelterEventDialogueController GetDialogueReference(
            DeskStoryObject legacy)
        {
            SerializedObject serialized = new SerializedObject(legacy);
            return serialized.FindProperty("eventDialogue").objectReferenceValue
                as ShelterEventDialogueController;
        }

        private static Scene FindLoadedScene(string path)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.path == path)
                {
                    return scene;
                }
            }
            return default;
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == objectName)
                    {
                        return candidate.gameObject;
                    }
                }
            }
            return null;
        }
    }
}
