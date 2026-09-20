#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.CameraSystem;
using YesterdayMap.Audio;
using YesterdayMap.BranchOne;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.EditorTools;
using YesterdayMap.Exploration;
using YesterdayMap.Resources;
using YesterdayMap.Scavenge;
using YesterdayMap.Shelter;
using YesterdayMap.UI;

namespace YesterdayMap.Editor
{
    public static class V02ProjectBuilder
    {
        private const string Root = "Assets/_Project";
        private const string MaterialRoot = Root + "/Materials";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string SceneRoot = Root + "/Scenes";
        private const string MeshyHousePrefabPath = PrefabRoot + "/Map/HouseInterior_Meshy.prefab";
        private const string BunkerMapPrefabPath = PrefabRoot + "/Environment/BunkerMap.prefab";
        private const string MeshyShelterBunkerPrefabPath = PrefabRoot + "/Environment/MeshyBunkerEntrance.prefab";
        private const string MeshyBunkerDoorPrefabPath = PrefabRoot + "/Environment/MeshyBunkerDoor.prefab";
        private const string MeshyDrainagePumpPrefabPath = PrefabRoot + "/Environment/MeshyDrainagePump.prefab";
        private const string BipedPlayerPrefabPath = PrefabRoot + "/Characters/HanDoyoon_UrbanSurvivalistBiped.prefab";
        private const string ScavengeItemAtlasPath = Root + "/Art/Items/ScavengeItemAtlas.png";
        private const string ScavengeInventoryFramePath = SceneRoot + "/UI/inventory.png";
        private const string ScavengeClockArtworkPath = SceneRoot + "/UI/timer.png";
        private const string DiaryIconPath = SceneRoot + "/UI/diary.png";
        private const string DiaryViewPath = SceneRoot + "/UI/diary_view.png";
        private const string DiaryContentFontPath = SceneRoot + "/font/UhBee ibuson.ttf";
        private const string DefaultUIFontPath = SceneRoot + "/font/SUIT-Regular.otf";
        private const string ExplorationMapPath = SceneRoot + "/UI/exploration_map.png";
        private const string LastBunkerEndingRoot = Root + "/Art/UI/Ending/LastBunker";
        private const string GameSettingsPanelPrefabPath = PrefabRoot + "/UI/GameSettingsPanel.prefab";
        private const string MainMenuMusicPath = Root + "/Audio/The Void - Oles Flat.mp3";
        private const string FoodVisualPrefabPath = PrefabRoot + "/Items/SurvivalFood.prefab";
        private const string WaterVisualPrefabPath = PrefabRoot + "/Items/BottledWater.prefab";
        private const string MedicineVisualPrefabPath = PrefabRoot + "/Items/FirstAidKit.prefab";

        private const float ScavengeMapScale = 3f;
        private static readonly Dictionary<string, Material> Materials = new();
        private static Font font;

        [MenuItem("Yesterday Map/Build v0.2 2.5D Project")]
        public static void BuildAll()
        {
            EnsureFolders();
            CreateMaterials();
            Dictionary<string, GameObject> prefabs = CreatePrefabs();
            ExplorationLocationData[] locations = CreateExplorationData();
            BuildMainMenuScene();
            BuildPrologueScene();
            BuildScavengeScene(prefabs);
            BuildShelterScene(prefabs, locations);
            BuildExplorationScene();
            EditorBuildSettings.scenes = new[]
            {
                BuildScene("MainMenu"), BuildScene("Prologue"), BuildScene("Scavenge"),
                BuildScene("Shelter"), BuildScene("Exploration")
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Yesterday Map v0.2: five scenes and 3D prefabs created.");
        }

        [MenuItem("Yesterday Map/Install Last Bunker Ending Sequence")]
        public static void InstallLastBunkerEndingSequence()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException("Shelter Canvas를 찾을 수 없습니다.");

            LastBunkerEndingSequence existing =
                Object.FindFirstObjectByType<LastBunkerEndingSequence>(
                    FindObjectsInactive.Include);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            CreateLastBunkerEndingSequence(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LAST_BUNKER_ENDING_SEQUENCE_INSTALLED");
        }

        [MenuItem("Yesterday Map/Install Signal Ending Sequences")]
        public static void InstallSignalEndingSequences()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException(
                    "Shelter Canvas를 찾을 수 없습니다.");

            SignalEndingSequence existing =
                Object.FindFirstObjectByType<SignalEndingSequence>(
                    FindObjectsInactive.Include);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            CreateSignalEndingSequence(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SIGNAL_ENDING_SEQUENCES_INSTALLED");
        }

        [MenuItem("Yesterday Map/Install Join Ending Sequences")]
        public static void InstallJoinEndingSequences()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException(
                    "Shelter Canvas를 찾을 수 없습니다.");

            JoinEndingSequence existing =
                Object.FindFirstObjectByType<JoinEndingSequence>(
                    FindObjectsInactive.Include);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            CreateJoinEndingSequence(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("JOIN_ENDING_SEQUENCES_INSTALLED");
        }

        [MenuItem("Yesterday Map/Install Temporary Last Bunker Ending Button")]
        public static void InstallTemporaryLastBunkerEndingButton()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException("Shelter Canvas를 찾을 수 없습니다.");

            Transform oldButton = canvas.transform.Find(
                "TempLastBunkerEndingButton");
            if (oldButton != null) Object.DestroyImmediate(oldButton.gameObject);
            CreateTemporaryLastBunkerEndingButton(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TEMP_LAST_BUNKER_ENDING_BUTTON_INSTALLED");
        }

        [MenuItem("Yesterday Map/Install Temporary Quarter 3 Ending Eve Buttons")]
        public static void InstallTemporaryQuarter3EndingEveButtons()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException(
                    "Shelter Canvas를 찾을 수 없습니다.");

            Transform oldSurvivorButton = canvas.transform.Find(
                "TempSurvivorJoinEndingEveButton");
            if (oldSurvivorButton != null)
                Object.DestroyImmediate(oldSurvivorButton.gameObject);
            Transform oldRedArmbandButton = canvas.transform.Find(
                "TempRedArmbandJoinEndingEveButton");
            if (oldRedArmbandButton != null)
                Object.DestroyImmediate(oldRedArmbandButton.gameObject);
            Transform oldSignalButton = canvas.transform.Find(
                "TempSignalEndingEveButton");
            if (oldSignalButton != null)
                Object.DestroyImmediate(oldSignalButton.gameObject);
            Transform oldJoinButton = canvas.transform.Find(
                "TempJoinEndingEveButton");
            if (oldJoinButton != null)
                Object.DestroyImmediate(oldJoinButton.gameObject);
            CreateTemporaryQuarter3EndingEveButtons(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TEMP_QUARTER3_ENDING_EVE_BUTTONS_INSTALLED");
        }

        [MenuItem("Yesterday Map/Install Last Survival Record Ending Test")]
        public static void InstallLastSurvivalRecordEndingTest()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException("Shelter Canvas를 찾을 수 없습니다.");

            LastSurvivalRecordEndingSequence existing =
                Object.FindFirstObjectByType<LastSurvivalRecordEndingSequence>(
                    FindObjectsInactive.Include);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            CreateLastSurvivalRecordEndingSequence(canvas.transform);

            Transform oldButton = canvas.transform.Find(
                "TempLastSurvivalRecordButton");
            if (oldButton != null) Object.DestroyImmediate(oldButton.gameObject);
            CreateTemporaryLastSurvivalRecordButton(canvas.transform);
            Transform oldWaterButton = canvas.transform.Find(
                "TempDehydrationEndingButton");
            if (oldWaterButton != null)
                Object.DestroyImmediate(oldWaterButton.gameObject);
            CreateTemporaryDehydrationEndingButton(canvas.transform);
            CreateTemporaryQuarter3EndingEveButtons(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("LAST_SURVIVAL_RECORD_ENDING_TEST_INSTALLED");
        }

        [MenuItem("Yesterday Map/Rebuild Main Menu Scene")]
        public static void RebuildMainMenu()
        {
            EnsureFolders();
            CreateMaterials();
            BuildMainMenuScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Yesterday Map: MainMenu scene rebuilt.");
        }

        [MenuItem("Yesterday Map/Install Main Menu Music")]
        public static void InstallMainMenuMusic()
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/MainMenu.unity", OpenSceneMode.Single);
            MainMenuMusic existing = Object.FindFirstObjectByType<MainMenuMusic>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            CreateMainMenuMusic();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MAIN_MENU_MUSIC_INSTALLED");
        }

        [MenuItem("Yesterday Map/Rebuild Scavenge Scene")]
        public static void RebuildScavengeScene()
        {
            EnsureFolders();
            CreateMaterials();
            Dictionary<string, GameObject> prefabs = CreatePrefabs();
            BuildScavengeScene(prefabs);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Yesterday Map: Scavenge scene rebuilt.");
        }

        [MenuItem("Yesterday Map/Rebuild Exploration Scene")]
        public static void RebuildExplorationScene()
        {
            EnsureFolders();
            BuildExplorationScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Yesterday Map: Exploration scene rebuilt.");
        }

        [MenuItem("Yesterday Map/Install Exploration Scene Feature")]
        public static void InstallExplorationSceneFeature()
        {
            EnsureFolders();
            BuildExplorationScene();

            Scene shelterScene = EditorSceneManager.OpenScene(
                $"{SceneRoot}/Shelter.unity", OpenSceneMode.Single);
            DoorObject door = Object.FindFirstObjectByType<DoorObject>();
            ExplorationManager manager = Object.FindFirstObjectByType<ExplorationManager>();
            Canvas shelterCanvas = Object.FindFirstObjectByType<Canvas>();
            SceneFader fader = Object.FindFirstObjectByType<SceneFader>();

            if (door == null || manager == null || shelterCanvas == null)
                throw new System.InvalidOperationException(
                    "Shelter 씬에서 DoorObject, ExplorationManager 또는 Canvas를 찾을 수 없습니다.");

            if (fader == null) fader = CreateFader(shelterCanvas.transform);
            else fader.transform.SetAsLastSibling();
            door.Configure(manager, fader);
            EditorUtility.SetDirty(door);
            EditorSceneManager.MarkSceneDirty(shelterScene);
            EditorSceneManager.SaveScene(shelterScene);

            List<EditorBuildSettingsScene> buildScenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string explorationPath = $"{SceneRoot}/Exploration.unity";
            if (!buildScenes.Exists(entry => entry.path == explorationPath))
                buildScenes.Add(new EditorBuildSettingsScene(explorationPath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("EXPLORATION_SCENE_FEATURE_INSTALLED");
        }

        private static EditorBuildSettingsScene BuildScene(string name) => new($"{SceneRoot}/{name}.unity", true);

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Materials", "Scenes", "Data", "Prefabs", "Prefabs/Characters", "Prefabs/Facilities",
                "Prefabs/Environment", "Prefabs/Map", "Scripts/Camera", "Scripts/Scavenge"
            };
            foreach (string relative in folders)
            {
                string current = Root;
                foreach (string part in relative.Split('/'))
                {
                    string next = current + "/" + part;
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                    current = next;
                }
            }
            font = AssetDatabase.LoadAssetAtPath<Font>(DefaultUIFontPath);
            if (font == null)
                throw new System.InvalidOperationException($"기본 UI 폰트를 찾을 수 없습니다: {DefaultUIFontPath}");
        }

        private static void CreateMaterials()
        {
            CreateMaterial("FloorConcrete", "30383B");
            CreateMaterial("WallConcrete", "50585B");
            CreateMaterial("DarkMetal", "273238", 0.72f);
            CreateMaterial("BedFabric", "496C7A");
            CreateMaterial("EmergencyRed", "9D493E", 0.35f);
            CreateMaterial("WaterBlue", "3C8791", 0.55f);
            CreateMaterial("GeneratorYellow", "A67A38", 0.5f);
            CreateMaterial("Wood", "71523A", 0.1f);
            CreateMaterial("StorageGreen", "506B57", 0.25f);
            CreateMaterial("PlayerJacket", "C9A354", 0.1f);
            CreateMaterial("Skin", "C88E70", 0.05f);
            CreateMaterial("Selection", "E8B84D", 0.15f, true);
            CreateMaterial("HouseFloor", "6F6659");
            // 맵 위 수집물은 멀리서도 종류를 구분할 수 있도록 선명한 임시 색을 사용한다.
            CreateMaterial("ItemFood", "3E8E4E");
            CreateMaterial("ItemWater", "3B82C4");
            CreateMaterial("ItemMedicine", "B83A36");
            CreateMaterial("ItemWhite", "F2F2E8");
            CreateMaterial("ItemParts", "707B83");
            CreateMaterial("ItemBattery", "73905C");
            CreateScavengeItemMaterials();
        }

        private static Material CreateMaterial(string name, string hex, float metallic = 0f, bool emission = false)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            // 초기 프로토타입은 Built-in Standard 셰이더로 충분하며 빌드 변형 수도 작다.
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            material.shader = shader;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", metallic * 0.6f);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.8f);
            }
            Materials[name] = material;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateScavengeItemMaterials()
        {
            TextureImporter importer = AssetImporter.GetAtPath(ScavengeItemAtlasPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ScavengeItemAtlasPath);
            if (atlas == null)
            {
                Debug.LogWarning($"수집 아이템 아이콘 시트를 찾지 못했습니다: {ScavengeItemAtlasPath}");
                return;
            }

            CreateAtlasMaterial("ScavengeFood", atlas, 0);
            CreateAtlasMaterial("ScavengeWater", atlas, 1);
            CreateAtlasMaterial("ScavengeMedicine", atlas, 2);
        }

        private static void CreateAtlasMaterial(string name, Texture2D atlas, int atlasCell)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Shader shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = Color.white;
            material.mainTexture = atlas;
            material.mainTextureScale = new Vector2(1f / 3f, 1f);
            material.mainTextureOffset = new Vector2(atlasCell / 3f, 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            Materials[name] = material;
            EditorUtility.SetDirty(material);
        }

        private static Dictionary<string, GameObject> CreatePrefabs()
        {
            Dictionary<string, GameObject> result = new();
            result["Player"] = CreatePlayerPrefab();
            result["Bed"] = CreateFacilityPrefab<BedObject>("Bed", new Vector3(2.4f, 0.8f, 1.25f), "BedFabric");
            result["WaterPurifier"] = CreateFacilityPrefab<WaterPurifierObject>("WaterPurifier", new Vector3(1.25f, 1.8f, 1.1f), "WaterBlue");
            result["Generator"] = CreateFacilityPrefab<GeneratorObject>("Generator", new Vector3(1.8f, 1.25f, 1.3f), "GeneratorYellow");
            result["Radio"] = CreateFacilityPrefab<RadioObject>("Radio", new Vector3(1.35f, 1.3f, 0.9f), "DarkMetal");
            result["Storage"] = CreateFacilityPrefab<StorageObject>("Storage", new Vector3(2f, 2f, 0.9f), "StorageGreen");
            result["Workbench"] = CreateFacilityPrefab<WorkbenchObject>("Workbench", new Vector3(2.4f, 1.15f, 1.1f), "Wood");
            result["ExitDoor"] = CreateFacilityPrefab<DoorObject>("ExitDoor", new Vector3(2.2f, 2.7f, 0.35f), "EmergencyRed");
            result["Barricade"] = CreateFacilityPrefab<BarricadeObject>("Barricade", new Vector3(4.2f, 1.8f, 0.5f), "DarkMetal");
            result["WallSegment"] = CreateEnvironmentPrefab("WallSegment", new Vector3(4f, 2.8f, 0.35f), "WallConcrete");
            return result;
        }

        private static GameObject CreatePlayerPrefab()
        {
            GameObject root = new("HanDoyoon");
            root.transform.localScale = Vector3.one * 2f;
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            // 외형은 2배지만 충돌체까지 2배가 되면 좁은 출입구와 겹쳐 물리적으로 밀려난다.
            // 루트 스케일을 고려해 로컬 충돌체를 절반으로 두어 실제 크기는 보통 사람 크기로 유지한다.
            capsule.height = 0.8f; capsule.radius = 0.175f; capsule.center = new Vector3(0, 0.4f, 0);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            root.AddComponent<CharacterStats>(); root.AddComponent<PlayerSelection>(); root.AddComponent<CharacterFootstepAudio>(); root.AddComponent<PlayerMovement>(); root.AddComponent<PlayerInteraction>();
            root.AddComponent<PlayerCarryController>();

            GameObject marker = CreatePart(root.transform, PrimitiveType.Cylinder, "SelectionMarker", new Vector3(0, 0.03f, 0), new Vector3(0.75f, 0.015f, 0.75f), "Selection");
            marker.SetActive(true);

            // 걷기 애니메이션이 포함된 Meshy 캐릭터 모델이 있으면 임시 캡슐 대신 시각 모델로 사용한다.
            GameObject bipedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BipedPlayerPrefabPath);
            Transform bipedVisualSource = bipedPrefab != null ? bipedPrefab.transform.Find("UrbanSurvivalistBiped_Model") : null;
            if (bipedVisualSource != null)
            {
                GameObject bipedVisual = Object.Instantiate(bipedVisualSource.gameObject, root.transform);
                bipedVisual.name = "UrbanSurvivalistBiped_Model";
                bipedVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                bipedVisual.transform.localScale = Vector3.one;
            }
            else
            {
                // Keep primitive visuals only as an import-failure fallback.
                CreatePart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0, 0.85f, 0), new Vector3(0.65f, 0.8f, 0.55f), "PlayerJacket");
                CreatePart(root.transform, PrimitiveType.Sphere, "Head", new Vector3(0, 1.72f, 0), Vector3.one * 0.46f, "Skin");
            }

            return SavePrefab(root, $"{PrefabRoot}/Characters/HanDoyoon.prefab");
        }

        private static GameObject CreateFacilityPrefab<T>(string name, Vector3 size, string material) where T : ShelterObject
        {
            GameObject root = new(name);
            BoxCollider collider = root.AddComponent<BoxCollider>(); collider.size = size; collider.center = new Vector3(0, size.y * 0.5f, 0);
            T facility = root.AddComponent<T>();
            CreatePart(root.transform, PrimitiveType.Cube, name + "Body", new Vector3(0, size.y * 0.5f, 0), size, material);
            Transform interactionPoint = CreateInteractionPoint(root.transform, GetFacilityInteractionPoint(name, size));
            facility.SetInteractionPoint(interactionPoint);
            if (name == "Bed")
            {
                CreatePart(root.transform, PrimitiveType.Cube, "Pillow", new Vector3(0, 0.9f, 0.38f), new Vector3(1.8f, 0.22f, 0.35f), "FloorConcrete");
                CreatePart(root.transform, PrimitiveType.Cube, "BedFrame", new Vector3(0, 0.35f, 0), new Vector3(2.55f, 0.18f, 1.38f), "DarkMetal");
            }
            else if (name == "WaterPurifier")
            {
                CreatePart(root.transform, PrimitiveType.Cylinder, "FilterTank", new Vector3(0, 1.1f, -0.25f), new Vector3(0.38f, 0.72f, 0.38f), "DarkMetal");
            }
            else if (name == "Generator")
            {
                CreatePart(root.transform, PrimitiveType.Cylinder, "Engine", new Vector3(0, 0.9f, 0), new Vector3(0.48f, 0.72f, 0.48f), "DarkMetal", new Vector3(0, 0, 90));
            }
            else if (name == "Radio")
            {
                CreatePart(root.transform, PrimitiveType.Cylinder, "Antenna", new Vector3(0.42f, 2f, 0), new Vector3(0.04f, 0.75f, 0.04f), "DarkMetal");
            }
            else if (name == "Storage")
            {
                CreatePart(root.transform, PrimitiveType.Cube, "LeftDoor", new Vector3(-0.48f, 1.1f, -0.48f), new Vector3(0.88f, 1.75f, 0.08f), "DarkMetal");
                CreatePart(root.transform, PrimitiveType.Cube, "RightDoor", new Vector3(0.48f, 1.1f, -0.48f), new Vector3(0.88f, 1.75f, 0.08f), "DarkMetal");
            }
            else if (name == "Workbench")
            {
                CreatePart(root.transform, PrimitiveType.Cube, "BackBoard", new Vector3(0, 1.55f, 0.42f), new Vector3(2.2f, 1.1f, 0.15f), "DarkMetal");
            }
            else if (name == "Barricade")
            {
                for (int i = -2; i <= 2; i++) CreatePart(root.transform, PrimitiveType.Cube, $"SteelBar_{i + 3}", new Vector3(i * 0.75f, 1f, -0.28f), new Vector3(0.22f, 2f, 0.25f), "DarkMetal");
            }
            return SavePrefab(root, $"{PrefabRoot}/Facilities/{name}.prefab");
        }

        private static Transform CreateInteractionPoint(Transform parent, Vector3 localPosition)
        {
            GameObject point = new("InteractionPoint");
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            return point.transform;
        }

        private static Vector3 GetFacilityInteractionPoint(string name, Vector3 size)
        {
            float xGap = size.x * 0.5f + 0.85f;
            float zGap = size.z * 0.5f + 0.85f;
            return name switch
            {
                "WaterPurifier" or "Generator" => new Vector3(xGap, 0f, 0f),
                "Radio" or "Storage" or "Workbench" => new Vector3(-xGap, 0f, 0f),
                "ExitDoor" => new Vector3(0f, 0f, zGap),
                "Barricade" or "Bed" => new Vector3(0f, 0f, -zGap),
                _ => new Vector3(0f, 0f, -zGap)
            };
        }

        private static GameObject CreateEnvironmentPrefab(string name, Vector3 size, string material)
        {
            GameObject root = new(name);
            BoxCollider collider = root.AddComponent<BoxCollider>(); collider.size = size; collider.center = new Vector3(0, size.y * 0.5f, 0);
            CreatePart(root.transform, PrimitiveType.Cube, "ConcreteMesh", new Vector3(0, size.y * 0.5f, 0), size, material);
            return SavePrefab(root, $"{PrefabRoot}/Environment/{name}.prefab");
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, string material, Vector3 rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(type); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale; part.transform.localEulerAngles = rotation;
            Collider collider = part.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
            part.GetComponent<Renderer>().sharedMaterial = Materials[material];
            return part;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static ExplorationLocationData[] CreateExplorationData()
        {
            return new[]
            {
                CreateLocation("ConvenienceStore", "편의점", 0.15f, 1, "식량, 식수", new[]
                {
                    Reward(ResourceType.Food, 0, 2, 50f, Amounts(10f, 80f, 10f), Amounts(0f, 70f, 30f)),
                    Reward(ResourceType.Water, 0, 2, 50f, Amounts(10f, 80f, 10f), Amounts(0f, 70f, 30f))
                }),
                CreateLocation("Hospital", "병원", 0.25f, 3, "약품, 식수", new[]
                {
                    Reward(ResourceType.Medicine, 1, 1, 40f, Amounts(0f, 100f), Amounts(0f, 100f)),
                    Reward(ResourceType.Water, 0, 2, 60f, Amounts(30f, 60f, 10f), Amounts(10f, 70f, 20f))
                }),
                CreateLocation("ResidentialArea", "주택가", 0.20f, 2, "식량, 식수, 연료", new[]
                {
                    Reward(ResourceType.Food, 0, 2, 40f, Amounts(20f, 70f, 10f), Amounts(0f, 80f, 20f)),
                    Reward(ResourceType.Water, 0, 2, 40f, Amounts(20f, 70f, 10f), Amounts(0f, 80f, 20f)),
                    Reward(ResourceType.Fuel, 0, 1, 20f, Amounts(50f, 50f), Amounts(50f, 50f))
                }),
                CreateLocation("PoliceStation", "경찰서", 0.30f, 4, "약품, 수리키트, 연료", new[]
                {
                    Reward(ResourceType.Medicine, 0, 1, 25f, Amounts(30f, 70f), Amounts(0f, 100f)),
                    Reward(ResourceType.Parts, 1, 3, 25f, Amounts(0f, 50f, 25f, 25f), Amounts(0f, 50f, 25f, 25f)),
                    Reward(ResourceType.Fuel, 0, 2, 50f, Amounts(20f, 60f, 20f), Amounts(0f, 60f, 40f))
                }),
                CreateLocation("CommunicationsStation", "통신소", 0.20f, 2, "수리키트, 연료", new[]
                {
                    Reward(ResourceType.Parts, 0, 5, 60f, Amounts(1f, 1f, 1f, 1f, 1f, 1f), Amounts(0f, 1f, 1f, 1f, 1f, 1f)),
                    Reward(ResourceType.Fuel, 0, 2, 40f, Amounts(20f, 60f, 20f), Amounts(0f, 60f, 40f))
                })
            };
        }

        private static ExplorationLocationData CreateLocation(string assetName, string displayName, float danger,
            int dangerStars, string summary, ExplorationLocationData.RewardEntry[] rewards)
        {
            string path = $"{Root}/Data/{assetName}.asset";
            ExplorationLocationData data = AssetDatabase.LoadAssetAtPath<ExplorationLocationData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<ExplorationLocationData>(); AssetDatabase.CreateAsset(data, path); }
            data.Configure(displayName, danger, rewards, summary, dangerStars); EditorUtility.SetDirty(data); return data;
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

        private static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UnityEngine.Camera camera = CreatePerspectiveCamera(new Vector3(0, 5, -12), new Vector3(15, 0, 0), ColorFromHex("080B0D"));
            CreateCityBackdrop();
            Canvas canvas = CreateCanvas("MainMenuUI");
            Image shade = CreatePanel(canvas.transform, "DarkOverlay", Vector2.zero, Vector2.one, new Color(0.01f, 0.02f, 0.025f, 0.58f));

            // 넘패드 4번(좌측 중앙) 위치에 타이틀/버튼을 한 덩어리로 묶는다.
            Image menuPanel = CreatePanel(shade.transform, "MenuPanel", new Vector2(0.06f, 0.18f), new Vector2(0.34f, 0.82f), new Color(0.04f, 0.067f, 0.078f, 0.55f));
            Text title = CreateText(menuPanel.transform, "GameTitle", "어제의 지도", 56, new Vector2(0f, 0.855f), new Vector2(1f, 1f), TextAnchor.MiddleLeft);
            title.color = ColorFromHex("E5D7B8");
            CreateText(menuPanel.transform, "Tagline", "어제 안전했던 길이 오늘도 안전하다는 보장은 없다.", 18, new Vector2(0f, 0.745f), new Vector2(1f, 0.845f), TextAnchor.UpperLeft);

            // 버튼들을 타이틀/태그라인과 분리된 그룹으로 묶고, 양옆에 여백을 살짝 둔다.
            RectTransform buttonGroup = CreateGroup(menuPanel.transform, "ButtonGroup", new Vector2(0f, 0.06f), new Vector2(1f, 0.7f));
            Button start = CreateButton(buttonGroup, "NewGameButton", "새로운 생존 시작", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 1f), ColorFromHex("7E3D37"));
            Button continueGame = CreateButton(buttonGroup, "ContinueButton", "이어서 하기", new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.79f), ColorFromHex("2E3639"));
            Button records = CreateButton(buttonGroup, "RecordsButton", "생존 기록", new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.58f), ColorFromHex("2E3639"));
            Button settings = CreateButton(buttonGroup, "SettingsButton", "설정", new Vector2(0.05f, 0.21f), new Vector2(0.95f, 0.37f), ColorFromHex("2E3639"));
            Button quit = CreateButton(buttonGroup, "QuitButton", "게임 종료", new Vector2(0.05f, 0f), new Vector2(0.95f, 0.16f), ColorFromHex("3A464B"));
            continueGame.interactable = true;
            records.interactable = true;
            settings.interactable = true;

            GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                GameSettingsPanelPrefabPath);
            if (settingsPrefab != null)
            {
                GameObject settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                    settingsPrefab, scene);
                settingsInstance.name = "GameSettingsPanel";
                settingsInstance.transform.SetParent(canvas.transform, false);
                RectTransform settingsRect = settingsInstance.GetComponent<RectTransform>();
                if (settingsRect != null)
                {
                    settingsRect.anchorMin = Vector2.zero;
                    settingsRect.anchorMax = Vector2.one;
                    settingsRect.offsetMin = Vector2.zero;
                    settingsRect.offsetMax = Vector2.zero;
                }
            }

            SceneFader fader = CreateFader(canvas.transform);
            GameObject managers = new("MainMenuManagers");
            new GameObject("GameSession").AddComponent<GameSession>();
            CreateMainMenuMusic();
            MainMenuController controller = managers.AddComponent<MainMenuController>();
            controller.Configure(start, continueGame, records, settings, quit, fader);
            managers.AddComponent<V02RuntimeFlowValidator>();
            camera.gameObject.name = "MainCamera";
            SaveScene(scene, "MainMenu");
        }

        private static MainMenuMusic CreateMainMenuMusic()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MainMenuMusicPath);
            if (clip == null)
                throw new System.InvalidOperationException(
                    $"Main menu music was not found: {MainMenuMusicPath}");

            GameObject musicObject = new("MainMenuMusic");
            musicObject.AddComponent<AudioSource>();
            MainMenuMusic music = musicObject.AddComponent<MainMenuMusic>();
            music.Configure(clip, 3f, 1f);
            return music;
        }

        private static void BuildPrologueScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreatePerspectiveCamera(new Vector3(0, 3, -8), new Vector3(10, 0, 0), Color.black);
            Canvas canvas = CreateCanvas("PrologueUI");
            Image background = CreatePanel(canvas.transform, "BlackBackground", Vector2.zero, Vector2.one, Color.black);
            Text story = CreateText(background.transform, "StoryText", string.Empty, 36, new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.68f), TextAnchor.MiddleCenter);
            CreateText(background.transform, "ContinueHint", "좌클릭 또는 Space", 20, new Vector2(0.38f, 0.12f), new Vector2(0.62f, 0.2f), TextAnchor.MiddleCenter).color = new Color(1, 1, 1, 0.55f);
            SceneFader fader = CreateFader(canvas.transform);
            GameObject managers = new("PrologueManagers");
            PrologueController controller = managers.AddComponent<PrologueController>(); controller.Configure(story, fader);
            PrologueCinematicSceneInstaller.InstallIntoCanvas(canvas.transform, controller);
            managers.AddComponent<V02RuntimeFlowValidator>();
            SaveScene(scene, "Prologue");
        }

        private static void BuildScavengeScene(Dictionary<string, GameObject> prefabs)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateHouseWorld();
            Canvas canvas = CreateCanvas("ScavengeUI");
            // 남은 시간 시계는 좌상단 위치를 유지하면서 기존보다 50% 크게 표시한다.
            RectTransform clockGroup = CreateGroup(canvas.transform, "ScavengeClock", new Vector2(0.035f, 0.605f), new Vector2(0.215f, 0.965f));
            Image clockArtwork = CreatePanel(clockGroup, "ClockArtwork", Vector2.zero, Vector2.one, Color.white);
            clockArtwork.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ScavengeClockArtworkPath);
            clockArtwork.preserveAspect = true;
            clockArtwork.raycastTarget = false;
            clockArtwork.rectTransform.localScale = Vector3.one * 1.2f;

            Image clockFace = CreatePanel(clockGroup, "ClockFace", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.clear);
            clockFace.rectTransform.anchoredPosition = new Vector2(0f, 5f);
            clockFace.rectTransform.sizeDelta = new Vector2(222f, 232f);
            clockFace.raycastTarget = false;

            Image clockFill = CreatePanel(clockFace.transform, "ClockFill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.62f, 0.12f, 0.09f, 0.72f));
            clockFill.rectTransform.anchoredPosition = new Vector2(0f, 7f);
            clockFill.rectTransform.sizeDelta = new Vector2(250f, 255f);
            clockFill.raycastTarget = false;
            Text clockSeconds = CreateText(clockFace.transform, "ClockSeconds", string.Empty, 42, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            clockSeconds.color = ColorFromHex("3A2A1E");
            RadialTimerUI radialTimer = clockGroup.gameObject.AddComponent<RadialTimerUI>();
            radialTimer.Configure(clockArtwork, clockFace, clockFill, clockSeconds);

            Text message = ScavengeGuidanceSceneInstaller.CreateGuidance(canvas.transform);
            Text promptText = CreateText(canvas.transform, "InteractionPrompt", string.Empty, 25, new Vector2(0.2f, 0.245f), new Vector2(0.8f, 0.365f), TextAnchor.MiddleCenter);
            InteractionPromptUI promptUI = canvas.gameObject.AddComponent<InteractionPromptUI>(); promptUI.Configure(promptText);
            SceneFader fader = CreateFader(canvas.transform);

            GameObject managers = new("ScavengeManagers");
            new GameObject("GameSessionFallback").AddComponent<GameSession>();
            ScavengeManager scavenge = managers.AddComponent<ScavengeManager>(); scavenge.Configure(null, message, radialTimer, fader);
            CreateScavengeCarryInterface(canvas.transform, scavenge);
            ScavengeStatusPrefabInstaller.AttachToCanvas(canvas.transform, scavenge);
            managers.AddComponent<V02RuntimeFlowValidator>();

            // 입구 MeshCollider와 겹치지 않도록 시작점을 현관 안쪽의 빈 공간에 둔다.
            GameObject player = InstantiatePrefab(prefabs["Player"], "HanDoyoon", new Vector3(0, 1.08f, -5f * ScavengeMapScale));
            // Match the camera controller's initial offset from HanDoyoon so the edit-time
            // Scene view and the first Play Mode frame use the same character-centered framing.
            UnityEngine.Camera camera = CreatePerspectiveCamera(new Vector3(0, 20.62f, -35.19f), new Vector3(55, 0, 0), ColorFromHex("111315"));
            ConfigurePlayer(player, camera, promptUI);
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.03f;
            camera.gameObject.AddComponent<IsometricCameraController>().ConfigureFirstPerson(player.transform);

            // Meshy 집의 방 배치에 맞춰 주방·욕실·창고 역할을 하는 구역으로 물자를 나눈다.
            CreateCollectible("CannedFood_A", ResourceType.Food, 1, ScaleScavengePosition(-4.9f, 3.25f), scavenge);
            CreateCollectible("CannedFood_B", ResourceType.Food, 1, ScaleScavengePosition(-5.7f, 1.7f), scavenge);
            CreateCollectible("CannedFood_C", ResourceType.Food, 1, ScaleScavengePosition(-4.8f, -2.2f), scavenge);
            CreateCollectible("BottledWater_A", ResourceType.Water, 1, ScaleScavengePosition(-3.9f, 2.25f), scavenge);
            CreateCollectible("BottledWater_B", ResourceType.Water, 1, ScaleScavengePosition(5.7f, -3.35f), scavenge);
            CreateCollectible("FirstAidKit_A", ResourceType.Medicine, 1, ScaleScavengePosition(4.8f, 3.0f), scavenge);
            CreateCollectible("FirstAidKit_B", ResourceType.Medicine, 1, ScaleScavengePosition(4.9f, -2.0f), scavenge);
            PlaceScavengeFurniture();

            GameObject entrance = CreateBox("BunkerEntrance", new Vector3(0, 0.15f, 4.35f * ScavengeMapScale), new Vector3(2.6f, 0.3f, 1.4f), "EmergencyRed", true);
            BunkerEntrance bunkerEntrance = entrance.AddComponent<BunkerEntrance>(); bunkerEntrance.Configure(scavenge);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MeshyHousePrefabPath) != null)
                entrance.GetComponent<MeshRenderer>().enabled = false;
            CreateBunkerEntranceMarker(entrance.transform);
            SaveScene(scene, "Scavenge");
        }

        private static void AttachMeshyBunkerDoorVisual(GameObject doorRoot)
        {
            GameObject bunkerDoorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                MeshyBunkerDoorPrefabPath);
            if (bunkerDoorPrefab == null)
            {
                return;
            }

            foreach (Renderer renderer in doorRoot.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(
                bunkerDoorPrefab);
            visual.name = "MeshyBunkerDoorVisual";
            visual.transform.SetParent(doorRoot.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            visual.transform.localRotation = Quaternion.identity;

            Vector3 parentScale = doorRoot.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        }

        private static void CreateBunkerEntranceMarker(Transform entrance)
        {
            const float markerScale = 0.009f;
            Vector3 markerOffset = new(0f, 2.2f, 0f);

            GameObject markerObject = new(
                "BunkerEntranceMarker",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(BobbingWorldMarker));

            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.position = entrance.position + markerOffset;
            markerRect.sizeDelta = new Vector2(160f, 160f);
            markerRect.localScale = Vector3.one * markerScale;

            Canvas markerCanvas = markerObject.GetComponent<Canvas>();
            markerCanvas.renderMode = RenderMode.WorldSpace;
            markerCanvas.overrideSorting = true;
            markerCanvas.sortingOrder = 50;

            Text arrow = CreateText(
                markerRect,
                "DownArrow",
                "\u2193",
                132,
                Vector2.zero,
                Vector2.one,
                TextAnchor.MiddleCenter);
            arrow.fontStyle = FontStyle.Bold;
            arrow.color = ColorFromHex("F5B942");
            arrow.raycastTarget = false;

            Outline outline = arrow.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.92f);
            outline.effectDistance = new Vector2(4f, -4f);

            markerObject.GetComponent<BobbingWorldMarker>().Configure(
                entrance,
                markerOffset,
                0.18f,
                2.4f);
        }

        private static void BuildShelterScene(Dictionary<string, GameObject> prefabs, ExplorationLocationData[] locations)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateShelterWorld();

            GameObject systems = new("ShelterManagers");
            CharacterStats stats;
            ResourceManager resources = systems.AddComponent<ResourceManager>();
            EndingManager endingManager = systems.AddComponent<EndingManager>();
            YesterdayMap.Core.GameManager gameManager = systems.AddComponent<YesterdayMap.Core.GameManager>();
            DayCycleManager dayCycle = systems.AddComponent<DayCycleManager>();
            EventManager eventManager = systems.AddComponent<EventManager>();
            ExplorationManager explorationManager = systems.AddComponent<ExplorationManager>();
            ShelterBootstrap bootstrap = systems.AddComponent<ShelterBootstrap>();
            new GameObject("GameSessionFallback").AddComponent<GameSession>();
            systems.AddComponent<V02RuntimeFlowValidator>();

            Canvas canvas = CreateCanvas("ShelterHUD");
            UIManager uiManager = canvas.gameObject.AddComponent<UIManager>();
            InteractionPromptUI promptUI = canvas.gameObject.AddComponent<InteractionPromptUI>();
            ExplorationUI explorationUI = canvas.gameObject.AddComponent<ExplorationUI>();
            StorageUI storageUI = canvas.gameObject.AddComponent<StorageUI>();
            WorkProgressUI workProgressUI = canvas.gameObject.AddComponent<WorkProgressUI>();
            EndingUI endingUI = canvas.gameObject.AddComponent<EndingUI>();
            canvas.gameObject.AddComponent<FuelCanCarryIndicator>();

            CreateShelterInterface(canvas.transform, out Text dayText, out Text bunkerText, out Text statusText, out Text resourceText,
                out Text promptText, out Text messageText, out Dropdown ration, out Button endDay,
                out GameObject explorationPanel, out Text explorationResult, out Toggle gearToggle, out Button[] locationButtons,
                out Text[] locationDetails, out Button closeExploration, out GameObject storagePanel, out Text storageText,
                out Button closeStorage, out GameObject workPanel, out Slider workSlider, out Text workLabel,
                out GameObject endingPanel, out Text endingTitle, out Text endingDetail);
            SceneFader sceneFader = CreateFader(canvas.transform);
            CreateLastBunkerEndingSequence(canvas.transform);
            CreateSignalEndingSequence(canvas.transform);
            CreateJoinEndingSequence(canvas.transform);
            CreateTemporaryLastBunkerEndingButton(canvas.transform);
            CreateLastSurvivalRecordEndingSequence(canvas.transform);
            CreateTemporaryLastSurvivalRecordButton(canvas.transform);
            CreateTemporaryDehydrationEndingButton(canvas.transform);

            BarricadeObject barricade = InstantiateFacility<BarricadeObject>(prefabs["Barricade"], "Barricade", new Vector3(0, 0.97f, 5.35f), Quaternion.identity, uiManager, "[E] 방어벽 수리");
            BedObject bed = InstantiateFacility<BedObject>(prefabs["Bed"], "Bed", new Vector3(-5.5f, 0.97f, 3.3f), Quaternion.identity, uiManager, "[E] 침대에서 쉬기");
            WaterPurifierObject purifier = InstantiateFacility<WaterPurifierObject>(prefabs["WaterPurifier"], "WaterPurifier", new Vector3(-10f, 0.97f, -2f), Quaternion.identity, uiManager, "[E] 식수 정수");
            GeneratorObject generator = InstantiateFacility<GeneratorObject>(prefabs["Generator"], "Generator", new Vector3(-5.4f, 0.97f, -3.5f), Quaternion.identity, uiManager, "[E] 발전기 가동");
            RadioObject radio = InstantiateFacility<RadioObject>(prefabs["Radio"], "Radio", new Vector3(5.7f, 0.97f, 3.5f), Quaternion.identity, uiManager, "[E] 라디오 송출");
            StorageObject storage = InstantiateFacility<StorageObject>(prefabs["Storage"], "Storage", new Vector3(5.7f, 0.97f, 0.8f), Quaternion.identity, uiManager, "[E] 창고 열기");
            WorkbenchObject workbench = InstantiateFacility<WorkbenchObject>(prefabs["Workbench"], "Workbench", new Vector3(5.5f, 0.97f, -2.5f), Quaternion.identity, uiManager, "[E] 작업대 사용");
            DoorObject door = InstantiateFacility<DoorObject>(prefabs["ExitDoor"], "ExitDoor", new Vector3(0, 0.97f, -14.5f), Quaternion.identity, uiManager, "[E] 탐사 지역 선택");
            AttachMeshyBunkerDoorVisual(door.gameObject);

            GameObject drainagePumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                MeshyDrainagePumpPrefabPath);
            if (drainagePumpPrefab != null)
            {
                GameObject drainagePump = InstantiatePrefab(
                    drainagePumpPrefab,
                    "DrainagePump",
                    new Vector3(7.8f, 0f, -4f));
                drainagePump.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
                drainagePump.transform.localScale = Vector3.one * 0.8f;
            }
            GameObject player = InstantiatePrefab(prefabs["Player"], "HanDoyoon", new Vector3(0, 0.97f, -4f));
            stats = player.GetComponent<CharacterStats>();
            UnityEngine.Camera camera = CreatePerspectiveCamera(new Vector3(0, 13.97f, -14f), new Vector3(55, 0, 0), ColorFromHex("101517"));
            ConfigurePlayer(player, camera, promptUI);
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.03f;
            camera.gameObject.AddComponent<IsometricCameraController>().ConfigureFirstPerson(player.transform);

            promptUI.Configure(promptText);
            endingUI.Configure(endingPanel, endingTitle, endingDetail); endingManager.Configure(endingUI);
            storageUI.Configure(storagePanel, storageText, closeStorage, resources);
            explorationUI.Configure(explorationPanel, explorationResult, gearToggle, locationButtons, locationDetails, locations, closeExploration, explorationManager);
            workProgressUI.Configure(workPanel, workSlider, workLabel);
            uiManager.Configure(dayCycle, stats, messageText, endDay, storageUI, explorationUI, workProgressUI);
            CreateDiaryInterface(canvas.transform, stats, dayCycle, uiManager, explorationManager, resources);
            gameManager.Configure(stats, endingManager);
            dayCycle.Configure(stats, resources, eventManager, gameManager, uiManager);
            explorationManager.Configure(resources, stats, dayCycle, explorationUI, locations);
            eventManager.Configure(dayCycle, barricade, generator, purifier, gameManager, uiManager);
            bootstrap.Configure(resources);
            barricade.Configure(resources, dayCycle); bed.Configure(stats, dayCycle); purifier.Configure(resources, dayCycle);
            generator.Configure(resources, dayCycle); radio.Configure(dayCycle); storage.Configure(resources, stats);
            workbench.Configure(resources, barricade, dayCycle, generator, purifier);
            door.Configure(explorationManager, sceneFader);

            endingPanel.SetActive(false); explorationPanel.SetActive(false); storagePanel.SetActive(false); workPanel.SetActive(false);
            SaveScene(scene, "Shelter");
        }

        private static void BuildExplorationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasObject = new("ExplorationUI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<KoreanFontApplicator>();

            Image map = CreatePanel(canvas.transform, "ExplorationMap", Vector2.zero, Vector2.one, Color.white);
            map.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExplorationMapPath);
            map.preserveAspect = true;
            map.raycastTarget = false;

            Text result = CreateText(canvas.transform, "ExplorationResult", "1일차", 25,
                new Vector2(0.31f, 0.875f), new Vector2(0.69f, 0.965f), TextAnchor.MiddleCenter);
            result.color = ColorFromHex("2E2118");
            result.fontStyle = FontStyle.Bold;

            Button[] locations =
            {
                CreateMapHotspot(canvas.transform, "ConvenienceStoreButton", "편의점 탐사", new Vector2(0.235f, 0.47f), new Vector2(0.345f, 0.72f)),
                CreateMapHotspot(canvas.transform, "HospitalButton", "병원 탐사", new Vector2(0.420f, 0.49f), new Vector2(0.535f, 0.73f)),
                CreateMapHotspot(canvas.transform, "ResidentialAreaButton", "주택가 탐사", new Vector2(0.315f, 0.19f), new Vector2(0.425f, 0.45f)),
                CreateMapHotspot(canvas.transform, "PoliceStationHotspot", "경찰서 탐사", new Vector2(0.615f, 0.47f), new Vector2(0.735f, 0.71f)),
                CreateMapHotspot(canvas.transform, "CommunicationsStationHotspot", "통신소 탐사", new Vector2(0.495f, 0.16f), new Vector2(0.61f, 0.44f))
            };

            Toggle gear = CreateToggle(canvas.transform, "GearToggle", "장비 사용",
                new Vector2(0.73f, 0.08f), new Vector2(0.84f, 0.14f));
            Button back = CreateButton(canvas.transform, "ReturnButton", "쉘터로 돌아가기",
                new Vector2(0.84f, 0.065f), new Vector2(0.97f, 0.14f), new Color(0.12f, 0.09f, 0.07f, 0.92f));

            ExplorationSceneController controller =
                new GameObject("ExplorationController").AddComponent<ExplorationSceneController>();
            controller.Configure(locations, gear, result, null, null, back);
            controller.ConfigureLocations(CreateExplorationData());
            ExplorationLoadingPrefabInstaller.AttachToCanvas(canvas.transform, controller);

            Image fadeImage = CreatePanel(canvas.transform, "SceneFade", Vector2.zero, Vector2.one, Color.black);
            fadeImage.transform.SetAsLastSibling();
            CanvasGroup fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;
            SceneFader fader = fadeImage.gameObject.AddComponent<SceneFader>();
            // 탐사 화면은 선택 UI이므로 긴 연출보다 빠른 조작 진입을 우선한다.
            fader.Configure(fadeGroup, 0.3f);

            SaveScene(scene, "Exploration");
            ExplorationSceneUIRepair.Repair();
        }

        private static Button CreateMapHotspot(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            Button button = CreateButton(parent, name, label, min, max, new Color(0.18f, 0.08f, 0.04f, 0.01f));
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.01f);
            colors.highlightedColor = new Color(0.95f, 0.68f, 0.25f, 0.32f);
            colors.pressedColor = new Color(0.95f, 0.48f, 0.18f, 0.5f);
            button.colors = colors;
            button.GetComponentInChildren<Text>().color = Color.clear;
            return button;
        }

        private static void CreateCityBackdrop()
        {
            CreateBox("Street", new Vector3(0, -0.3f, 8), new Vector3(32, 0.4f, 24), "DarkMetal", false);
            for (int i = -4; i <= 4; i++)
            {
                float height = 3f + Mathf.Abs(i % 3) * 2f;
                CreateBox($"RuinedBuilding_{i + 5:00}", new Vector3(i * 3.8f, height * 0.5f, 5 + Mathf.Abs(i % 2) * 3), new Vector3(3f, height, 3f), "WallConcrete", false);
            }
            Light moon = new GameObject("ColdMoonLight").AddComponent<Light>(); moon.type = LightType.Directional;
            moon.color = ColorFromHex("71839A"); moon.intensity = 0.7f; moon.transform.rotation = Quaternion.Euler(45, -25, 0);
        }

        private static void CreateHouseWorld()
        {
            GameObject meshyHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyHousePrefabPath);
            if (meshyHousePrefab != null)
            {
                GameObject environment = new("HouseEnvironment");
                GameObject house = InstantiatePrefab(meshyHousePrefab, "MeshyHouseInterior", Vector3.zero);
                house.transform.SetParent(environment.transform, true);
                // 가로·세로·높이를 같은 비율로 확장해 집 전체의 비례를 유지한다.
                house.transform.localScale = Vector3.one * ScavengeMapScale;
                CreateBunkerLighting("HouseCeilingLight", new Vector3(0, 4.5f, 0), ColorFromHex("F0D7A2"), 4.5f, 24f);
                CreateBunkerLighting("HouseLight_NorthWest", new Vector3(-12f, 4.2f, 8f), ColorFromHex("E9D2A4"), 3.2f, 16f);
                CreateBunkerLighting("HouseLight_NorthEast", new Vector3(12f, 4.2f, 8f), ColorFromHex("E9D2A4"), 3.2f, 16f);
                CreateBunkerLighting("HouseLight_SouthWest", new Vector3(-12f, 4.2f, -8f), ColorFromHex("D6E1E5"), 2.4f, 16f);
                CreateBunkerLighting("HouseLight_SouthEast", new Vector3(12f, 4.2f, -8f), ColorFromHex("D6E1E5"), 2.4f, 16f);
                RenderSettings.ambientLight = ColorFromHex("34383A");
                return;
            }

            // 외부 모델이 아직 임포트되지 않은 환경에서도 씬 생성 도구가 실패하지 않도록 남겨 둔 대체 구조다.
            CreateBox("HouseFloor", new Vector3(0, -0.15f, 0), new Vector3(15, 0.3f, 11), "HouseFloor", true);
            CreateBox("NorthWall", new Vector3(0, 1.5f, 5.5f), new Vector3(15.5f, 3f, 0.35f), "WallConcrete", true);
            CreateBox("SouthWallLeft", new Vector3(-4.8f, 1.5f, -5.5f), new Vector3(5.7f, 3f, 0.35f), "WallConcrete", true);
            CreateBox("SouthWallRight", new Vector3(4.8f, 1.5f, -5.5f), new Vector3(5.7f, 3f, 0.35f), "WallConcrete", true);
            CreateBox("WestWall", new Vector3(-7.5f, 1.5f, 0), new Vector3(0.35f, 3f, 11), "WallConcrete", true);
            CreateBox("EastWall", new Vector3(7.5f, 1.5f, 0), new Vector3(0.35f, 3f, 11), "WallConcrete", true);
            CreateBox("InteriorWallA", new Vector3(-2.4f, 1.2f, 2.1f), new Vector3(0.25f, 2.4f, 6), "WallConcrete", true);
            CreateBox("InteriorWallB", new Vector3(3.0f, 1.2f, -1.4f), new Vector3(5.5f, 2.4f, 0.25f), "WallConcrete", true);
            CreateBox("DiningTable", new Vector3(1.0f, 0.55f, 2.9f), new Vector3(2.5f, 0.2f, 1.3f), "Wood", true);
            CreateBox("KitchenCounter", new Vector3(-5.9f, 0.65f, 2.2f), new Vector3(1.2f, 1.3f, 4.5f), "StorageGreen", true);
            CreateBunkerLighting("HouseCeilingLight", new Vector3(0, 4.5f, -1), ColorFromHex("F0D7A2"), 4.5f, 18f);
            RenderSettings.ambientLight = ColorFromHex("34383A");
        }

        private static void CreateShelterWorld()
        {
            GameObject bunkerMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BunkerMapPrefabPath);
            if (bunkerMapPrefab != null)
            {
                // 기존 맵의 충돌 구조는 유지하되 화면에는 새 Meshy 벙커만 보이게 한다.
                GameObject collisionLayout = InstantiatePrefab(
                    bunkerMapPrefab,
                    "ShelterCollisionLayout",
                    Vector3.zero);
                collisionLayout.transform.localScale = new Vector3(1.75f, 1f, 2.6f);
                foreach (Renderer renderer in collisionLayout.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            }
            else
                CreateBox("BunkerFloor", new Vector3(0, -0.15f, 0), new Vector3(16, 0.3f, 12), "FloorConcrete", true);

            GameObject meshyBunkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                MeshyShelterBunkerPrefabPath);
            if (meshyBunkerPrefab != null)
            {
                GameObject meshyBunker = InstantiatePrefab(
                    meshyBunkerPrefab,
                    "MeshyShelterBunker",
                    Vector3.zero);
                meshyBunker.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                meshyBunker.transform.localScale = new Vector3(6.5f, 3.5f, 3.5f);
            }

            CreateBunkerLighting("EmergencyLightWest", new Vector3(-5.5f, 3.2f, 1.5f), ColorFromHex("FF9A68"), 5.5f, 11f);
            CreateBunkerLighting("EmergencyLightEast", new Vector3(5.5f, 3.2f, -1.5f), ColorFromHex("FF9A68"), 5.5f, 11f);
            Light ceiling = new GameObject("DimCeilingLight").AddComponent<Light>();
            ceiling.type = LightType.Directional;
            ceiling.color = ColorFromHex("718596");
            ceiling.intensity = 0.45f;
            ceiling.shadows = LightShadows.Soft;
            ceiling.transform.rotation = Quaternion.Euler(58, -18, 0);
            RenderSettings.ambientLight = ColorFromHex("20282C");
        }

        private static void CreateBunkerLighting(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject fixture = CreateBox(name, position, new Vector3(0.55f, 0.18f, 0.35f), "EmergencyRed", false);
            Light light = fixture.AddComponent<Light>(); light.type = LightType.Point; light.color = color;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft;
        }

        private static GameObject CreateCollectible(string name, ResourceType type, int amount, Vector3 position, ScavengeManager manager)
        {
            GameObject item = new(name);
            item.transform.position = position;
            BoxCollider collider = item.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            GameObject model = new("ItemModel");
            model.transform.SetParent(item.transform, false);

            string visualPrefabPath = type switch
            {
                ResourceType.Water => WaterVisualPrefabPath,
                ResourceType.Medicine => MedicineVisualPrefabPath,
                _ => FoodVisualPrefabPath
            };
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefabPath);

            if (visualPrefab != null)
            {
                // 실제 수집물 모델은 별도 프리팹으로 관리한다.
                // 씬을 다시 빌드해도 Meshy 모델과 재질이 유지되도록 여기서 불러온다.
                Object.DestroyImmediate(model);
                model = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, item.transform);
                model.name = "ItemModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                collider.center = Vector3.zero;
                collider.size = type switch
                {
                    ResourceType.Water => new Vector3(0.65f, 1.15f, 0.65f),
                    ResourceType.Medicine => new Vector3(1.3f, 1.05f, 0.85f),
                    _ => new Vector3(0.8f, 1.1f, 0.8f)
                };
            }
            else if (type == ResourceType.Water)
            {
                // 파란 물병: 몸통, 둥근 어깨, 마개를 단순한 3D 도형으로 조합한다.
                CreatePart(model.transform, PrimitiveType.Cylinder, "BottleBody", new Vector3(0f, 0.05f, 0f), new Vector3(0.38f, 0.5f, 0.38f), "ItemWater");
                CreatePart(model.transform, PrimitiveType.Sphere, "BottleShoulder", new Vector3(0f, 0.52f, 0f), new Vector3(0.42f, 0.25f, 0.42f), "ItemWater");
                CreatePart(model.transform, PrimitiveType.Cylinder, "BottleCap", new Vector3(0f, 0.72f, 0f), new Vector3(0.18f, 0.1f, 0.18f), "ItemWater");
                collider.center = new Vector3(0f, 0.2f, 0f);
                collider.size = new Vector3(0.9f, 1.45f, 0.9f);
            }
            else if (type == ResourceType.Medicine)
            {
                // 빨간 구급상자 위에 흰색 십자가를 얹어 한눈에 알아볼 수 있게 한다.
                CreatePart(model.transform, PrimitiveType.Cube, "FirstAidCase", new Vector3(0f, -0.15f, 0f), new Vector3(0.9f, 0.5f, 0.6f), "ItemMedicine");
                CreatePart(model.transform, PrimitiveType.Cube, "CrossHorizontal", new Vector3(0f, 0.12f, 0f), new Vector3(0.46f, 0.04f, 0.13f), "ItemWhite");
                CreatePart(model.transform, PrimitiveType.Cube, "CrossVertical", new Vector3(0f, 0.12f, 0f), new Vector3(0.13f, 0.04f, 0.46f), "ItemWhite");
                collider.center = new Vector3(0f, -0.1f, 0f);
                collider.size = new Vector3(1.1f, 0.75f, 0.8f);
            }
            else
            {
                // 초록 통조림: 초록 원통과 위·아래 금속 테두리로 만든다.
                CreatePart(model.transform, PrimitiveType.Cylinder, "CanBody", Vector3.zero, new Vector3(0.55f, 0.4f, 0.55f), "ItemFood");
                CreatePart(model.transform, PrimitiveType.Cylinder, "CanTop", new Vector3(0f, 0.42f, 0f), new Vector3(0.58f, 0.03f, 0.58f), "DarkMetal");
                CreatePart(model.transform, PrimitiveType.Cylinder, "CanBottom", new Vector3(0f, -0.42f, 0f), new Vector3(0.58f, 0.03f, 0.58f), "DarkMetal");
                collider.center = Vector3.zero;
                collider.size = new Vector3(1.15f, 1f, 1.15f);
            }

            ScavengeCollectible collectible = item.AddComponent<ScavengeCollectible>(); collectible.Configure(type, amount, manager);
            return item;
        }

        private static void PlaceScavengeFurniture()
        {
            GameObject root = new("EditableFurniture");
            string[] prefabPaths =
            {
                "Assets/_Project/Prefabs/Furniture/RusticLibraryCabinet.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticTableForTwo_A.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticTableForTwo_B.prefab",
                "Assets/_Project/Prefabs/Furniture/MidcenturyBeigeRefrigerator.prefab",
                "Assets/_Project/Prefabs/Furniture/DustyFrontLoadingWasher.prefab",
                "Assets/_Project/Prefabs/Furniture/WeatheredWorkbench.prefab",
                "Assets/_Project/Prefabs/Furniture/MountainLakeWallArt.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticWoodenArmoire.prefab",
                "Assets/_Project/Prefabs/Furniture/VerdantQuiltBed.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticNightstand.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticDarkWoodKitchenCabinet.prefab",
                "Assets/_Project/Prefabs/Furniture/CreamVintageBathtub.prefab",
                "Assets/_Project/Prefabs/Furniture/BeigeToilet.prefab",
                "Assets/_Project/Prefabs/Furniture/OakBathroomVanity.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticWoodenCoffeeTable_A.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticWoodenCoffeeTable_B.prefab",
                "Assets/_Project/Prefabs/Furniture/RusticWoodenCoffeeTable_C.prefab",
                "Assets/_Project/Prefabs/Furniture/BrownLeatherArmchair_A.prefab",
                "Assets/_Project/Prefabs/Furniture/BrownLeatherArmchair_B.prefab"
            };
            Vector3[] positions =
            {
                new Vector3(5.2f * ScavengeMapScale, 0f, 3.15f * ScavengeMapScale),
                new Vector3(-1.8f * ScavengeMapScale, 0f, 1.25f * ScavengeMapScale),
                new Vector3(1.8f * ScavengeMapScale, 0f, 1.25f * ScavengeMapScale),
                new Vector3(-5.35f * ScavengeMapScale, 0f, 3.2f * ScavengeMapScale),
                new Vector3(5.25f * ScavengeMapScale, 0f, -3.0f * ScavengeMapScale),
                new Vector3(-5.15f * ScavengeMapScale, 0f, -2.8f * ScavengeMapScale),
                new Vector3(0f, 1.25f, 3.85f * ScavengeMapScale),
                new Vector3(3.83f * ScavengeMapScale, 0f, 2.5f * ScavengeMapScale),
                new Vector3(2.73f * ScavengeMapScale, 0f, 1.37f * ScavengeMapScale),
                new Vector3(3.72f * ScavengeMapScale, 0f, 1.48f * ScavengeMapScale),
                new Vector3(-3.93f * ScavengeMapScale, 0f, 2.7f * ScavengeMapScale),
                new Vector3(3.75f * ScavengeMapScale, 0f, -1.75f * ScavengeMapScale),
                new Vector3(5.77f * ScavengeMapScale, 0f, -4.27f * ScavengeMapScale),
                new Vector3(6.4f * ScavengeMapScale, 0f, -1.83f * ScavengeMapScale),
                new Vector3(-1.4f * ScavengeMapScale, 0f, 2.67f * ScavengeMapScale),
                new Vector3(0f, 0f, 2.67f * ScavengeMapScale),
                new Vector3(1.4f * ScavengeMapScale, 0f, 2.67f * ScavengeMapScale),
                new Vector3(-0.83f * ScavengeMapScale, 0f, 3.73f * ScavengeMapScale),
                new Vector3(0.83f * ScavengeMapScale, 0f, 3.73f * ScavengeMapScale)
            };
            Vector3[] rotations =
            {
                new Vector3(0f, 180f, 0f),
                Vector3.zero,
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(0f, -90f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 180f, 0f),
                Vector3.zero,
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 90f, 0f),
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 180f, 0f)
            };

            for (int i = 0; i < prefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                    continue;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = prefab.name;
                instance.transform.SetParent(root.transform);
                instance.transform.position = positions[i];
                instance.transform.eulerAngles = rotations[i];
            }
        }

        private static Vector3 ScaleScavengePosition(float x, float z)
        {
            return new Vector3(x * ScavengeMapScale, 0.45f, z * ScavengeMapScale);
        }

        private static void CreateScavengeCarryInterface(Transform canvas, ScavengeManager manager)
        {
            // 파밍용 가방 이미지를 화면 6시 방향에 두고, 그림 속 네 칸 위에 아이콘을 겹친다.
            GameObject panelObject = new("CarryInventoryPanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvas, false);
            panelObject.transform.SetAsFirstSibling();
            RectTransform panelRect = (RectTransform)panelObject.transform;
            // 현재 씬의 하단 중앙 배치를 기준으로 가로와 세로를 각각 20% 확대한다.
            panelRect.anchorMin = new Vector2(0.284f, 0.006f);
            panelRect.anchorMax = new Vector2(0.716f, 0.24f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject frameObject = new("InventoryFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            frameObject.transform.SetParent(panelObject.transform, false);
            RectTransform frameRect = (RectTransform)frameObject.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            RawImage frame = frameObject.GetComponent<RawImage>();
            frame.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ScavengeInventoryFramePath);
            frame.uvRect = new Rect(0f, 0.06f, 1f, 0.55f);
            frame.raycastTarget = false;

            RawImage[] slots = new RawImage[4];
            for (int i = 0; i < slots.Length; i++)
            {
                float xMin = 0.105f + i * 0.20f;
                GameObject iconObject = new($"ItemIcon_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
                iconObject.transform.SetParent(panelObject.transform, false);
                RectTransform iconRect = (RectTransform)iconObject.transform;
                iconRect.anchorMin = new Vector2(xMin, 0.22f);
                iconRect.anchorMax = new Vector2(xMin + 0.17f, 0.74f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                AspectRatioFitter fitter = iconObject.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1f;

                slots[i] = iconObject.GetComponent<RawImage>();
                slots[i].enabled = false;
            }

            Text capacity = CreateText(
                panelObject.transform,
                "CarryCapacity",
                "운반 물자  0/4",
                19,
                new Vector2(0.35f, 0.78f),
                new Vector2(0.65f, 0.96f),
                TextAnchor.MiddleCenter);
            capacity.color = new Color(0.93f, 0.84f, 0.62f, 1f);

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ScavengeItemAtlasPath);
            ScavengeCarryUI carryUI = panelObject.AddComponent<ScavengeCarryUI>();
            carryUI.Configure(manager, atlas, slots, capacity);
        }

        private static void CreateDiaryInterface(
            Transform canvas,
            CharacterStats stats,
            DayCycleManager dayCycle,
            UIManager uiManager,
            ExplorationManager explorationManager,
            ResourceManager resources)
        {
            GameObject buttonObject = new(
                "DiaryButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(Outline),
                typeof(Button),
                typeof(DiaryUI));
            buttonObject.transform.SetParent(canvas, false);

            RectTransform buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.anchorMin = new Vector2(0.87f, 0.16f);
            buttonRect.anchorMax = new Vector2(0.98f, 0.36f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            RawImage diaryIcon = buttonObject.GetComponent<RawImage>();
            diaryIcon.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DiaryIconPath);
            diaryIcon.raycastTarget = true;

            Outline hoverOutline = buttonObject.GetComponent<Outline>();
            hoverOutline.effectColor = new Color(1f, 1f, 1f, 0.9f);
            hoverOutline.effectDistance = new Vector2(5f, -5f);
            hoverOutline.useGraphicAlpha = true;
            hoverOutline.enabled = false;

            Button diaryButton = buttonObject.GetComponent<Button>();
            diaryButton.targetGraphic = diaryIcon;
            diaryButton.transition = Selectable.Transition.None;

            Text diaryShortcut = CreateText(
                canvas,
                "DiaryShortcutHint",
                "[Q]",
                24,
                new Vector2(0.89f, 0.365f),
                new Vector2(0.96f, 0.415f),
                TextAnchor.MiddleCenter);
            AddSubtitleOutline(diaryShortcut);

            GameObject viewPanel = new(
                "DiaryViewPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            viewPanel.transform.SetParent(canvas, false);
            RectTransform panelRect = (RectTransform)viewPanel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image backdrop = viewPanel.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.86f);
            Button backdropButton = viewPanel.GetComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;

            GameObject pageObject = new(
                "DiaryPage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            pageObject.transform.SetParent(viewPanel.transform, false);
            RectTransform pageRect = (RectTransform)pageObject.transform;
            pageRect.anchorMin = new Vector2(0.2f, 0.04f);
            pageRect.anchorMax = new Vector2(0.8f, 0.96f);
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;

            RawImage page = pageObject.GetComponent<RawImage>();
            page.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DiaryViewPath);
            page.raycastTarget = true;
            AspectRatioFitter pageFitter = pageObject.GetComponent<AspectRatioFitter>();
            pageFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            pageFitter.aspectRatio = page.texture != null
                ? (float)page.texture.width / page.texture.height
                : 0.82f;

            Text sectionTitle = CreateText(
                pageObject.transform,
                "DiarySectionTitle",
                "플레이어 상태",
                40,
                new Vector2(0.17f, 0.79f),
                new Vector2(0.89f, 0.91f),
                TextAnchor.MiddleCenter);
            sectionTitle.color = new Color(0.19f, 0.12f, 0.07f, 1f);

            Text contentText = CreateText(
                pageObject.transform,
                "DiaryContent",
                string.Empty,
                30,
                new Vector2(0.18f, 0.14f),
                new Vector2(0.88f, 0.79f),
                TextAnchor.UpperLeft);
            contentText.color = new Color(0.16f, 0.1f, 0.06f, 1f);

            Font diaryContentFont = AssetDatabase.LoadAssetAtPath<Font>(
                DiaryContentFontPath);
            if (diaryContentFont != null)
            {
                sectionTitle.font = diaryContentFont;
                contentText.font = diaryContentFont;
            }

            Button statsTab = CreateButton(
                viewPanel.transform,
                "DiaryStatsTab",
                "상태 · 자원",
                new Vector2(0.72f, 0.7f),
                new Vector2(0.95f, 0.78f),
                new Color(0.18f, 0.14f, 0.1f, 0.92f));
            Button resourcesTab = CreateButton(
                viewPanel.transform,
                "DiaryCluesTab",
                "판단 단서",
                new Vector2(0.72f, 0.37f),
                new Vector2(0.95f, 0.45f),
                new Color(0.18f, 0.14f, 0.1f, 0.92f));
            Button mainTab = CreateButton(
                viewPanel.transform,
                "DiaryMainTab",
                "메인 일기",
                new Vector2(0.72f, 0.59f),
                new Vector2(0.95f, 0.67f),
                new Color(0.18f, 0.14f, 0.1f, 0.92f));
            Button explorationTab = CreateButton(
                viewPanel.transform,
                "DiaryExplorationTab",
                "탐사 기록",
                new Vector2(0.72f, 0.48f),
                new Vector2(0.95f, 0.56f),
                new Color(0.18f, 0.14f, 0.1f, 0.92f));
            Button nextPage = CreateButton(
                viewPanel.transform,
                "DiaryNextPageButton",
                "다음 카테고리  ▶",
                new Vector2(0.76f, 0.1f),
                new Vector2(0.95f, 0.18f),
                new Color(0.38f, 0.25f, 0.14f, 0.96f));
            Button closeButton = CreateButton(
                viewPanel.transform,
                "DiaryCloseButton",
                "닫기  ×",
                new Vector2(0.86f, 0.86f),
                new Vector2(0.96f, 0.94f),
                new Color(0.42f, 0.18f, 0.14f, 0.98f));

            nextPage.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(false);

            Text closeHint = CreateText(
                viewPanel.transform,
                "DiaryCloseHint",
                "Q/ESC 닫기 · ↑/↓ 카테고리 · ←/→ 내용 넘기기",
                19,
                new Vector2(0.3f, 0.01f),
                new Vector2(0.7f, 0.07f),
                TextAnchor.MiddleCenter);
            closeHint.color = new Color(0.85f, 0.82f, 0.74f, 0.9f);

            DiaryUI diaryUI = buttonObject.GetComponent<DiaryUI>();
            diaryUI.Configure(diaryButton, diaryIcon, hoverOutline, viewPanel, backdropButton);
            diaryUI.ConfigureContentFont(diaryContentFont);
            diaryUI.ConfigureCloseButton(closeButton);
            diaryUI.ConfigureContent(
                statsTab,
                resourcesTab,
                mainTab,
                explorationTab,
                nextPage,
                sectionTitle,
                contentText,
                stats,
                dayCycle,
                uiManager,
                explorationManager,
                resources);
            viewPanel.SetActive(false);
        }

        private static T InstantiateFacility<T>(GameObject prefab, string name, Vector3 position, Quaternion rotation, UIManager ui, string prompt) where T : ShelterObject
        {
            GameObject instance = InstantiatePrefab(prefab, name, position); instance.transform.rotation = rotation;
            T facility = instance.GetComponent<T>(); facility.ConfigureBase(ui, prompt); return facility;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, string name, Vector3 position)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name; instance.transform.position = position; return instance;
        }

        private static void ConfigurePlayer(GameObject player, UnityEngine.Camera camera, InteractionPromptUI promptUI)
        {
            Rigidbody body = player.GetComponent<Rigidbody>();
            CharacterStats stats = player.GetComponent<CharacterStats>();
            PlayerSelection selection = player.GetComponent<PlayerSelection>();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            GameObject marker = player.transform.Find("SelectionMarker").gameObject;
            selection.Configure(camera, marker);
            movement.Configure(camera, body, selection);
            interaction.Configure(promptUI);
        }

        private static UnityEngine.Camera CreatePerspectiveCamera(Vector3 position, Vector3 rotation, Color background)
        {
            GameObject go = new("MainCamera"); go.tag = "MainCamera"; go.transform.position = position; go.transform.eulerAngles = rotation;
            UnityEngine.Camera camera = go.AddComponent<UnityEngine.Camera>(); camera.orthographic = false; camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = background; return camera;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, string material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObject.AddComponent<GraphicRaycaster>(); canvasObject.AddComponent<KoreanFontApplicator>();
            GameObject eventObject = new("EventSystem"); eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<StandaloneInputModule>();
            return canvas;
        }

        private static SceneFader CreateFader(Transform canvas)
        {
            Image image = CreatePanel(canvas, "SceneFade", Vector2.zero, Vector2.one, Color.black);
            image.transform.SetAsLastSibling();
            CanvasGroup group = image.gameObject.AddComponent<CanvasGroup>(); group.alpha = 1f;
            SceneFader fader = image.gameObject.AddComponent<SceneFader>(); fader.Configure(group); return fader;
        }

        private static LastBunkerEndingSequence CreateLastBunkerEndingSequence(
            Transform canvas)
        {
            Image background = CreatePanel(
                canvas,
                "LastBunkerEndingSequence",
                Vector2.zero,
                Vector2.one,
                Color.black);
            background.raycastTarget = true;

            CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.interactable = true;

            GameObject artworkObject = new("TimeLapseArtwork");
            artworkObject.transform.SetParent(background.transform, false);
            RectTransform artworkRect = artworkObject.AddComponent<RectTransform>();
            artworkRect.anchorMin = Vector2.zero;
            artworkRect.anchorMax = Vector2.one;
            artworkRect.offsetMin = Vector2.zero;
            artworkRect.offsetMax = Vector2.zero;
            RawImage artwork = artworkObject.AddComponent<RawImage>();
            artwork.raycastTarget = false;
            AspectRatioFitter fitter = artworkObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1672f / 941f;

            Texture[] images =
            {
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/later1.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/later10.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/later30.png")
            };

            LastBunkerEndingSequence sequence =
                background.gameObject.AddComponent<LastBunkerEndingSequence>();
            Text prompt = CreateText(
                background.transform,
                "ContinuePrompt",
                "아무 키나 눌러 계속하세요",
                28,
                new Vector2(0.2f, 0.08f),
                new Vector2(0.8f, 0.18f),
                TextAnchor.MiddleCenter);
            prompt.color = new Color(1f, 1f, 1f, 0.9f);
            prompt.raycastTarget = false;
            prompt.gameObject.SetActive(false);
            sequence.Configure(artwork, group, images, prompt);
            background.gameObject.SetActive(false);
            return sequence;
        }

        private static SignalEndingSequence CreateSignalEndingSequence(
            Transform canvas)
        {
            Image background = CreatePanel(
                canvas,
                "SignalEndingSequence",
                Vector2.zero,
                Vector2.one,
                Color.black);
            background.raycastTarget = true;

            CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.interactable = true;

            GameObject artworkObject = new("SignalEndingArtwork");
            artworkObject.transform.SetParent(background.transform, false);
            RectTransform artworkRect = artworkObject.AddComponent<RectTransform>();
            artworkRect.anchorMin = Vector2.zero;
            artworkRect.anchorMax = Vector2.one;
            artworkRect.offsetMin = Vector2.zero;
            artworkRect.offsetMax = Vector2.zero;
            RawImage artwork = artworkObject.AddComponent<RawImage>();
            artwork.raycastTarget = false;
            AspectRatioFitter fitter = artworkObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1672f / 941f;

            Texture[] militaryImages =
            {
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/soldier radio/soldier.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/soldier radio/soldier2.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/soldier radio/soldier3.png")
            };
            Texture[] badRadioImages =
            {
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/bad radio/bad radio1.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/bad radio/bad radio2.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/bad radio/bad radio3.png")
            };

            SignalEndingSequence sequence =
                background.gameObject.AddComponent<SignalEndingSequence>();
            Text caption = CreateEndingCaption(background.transform);
            Text prompt = CreateText(
                background.transform,
                "ContinuePrompt",
                "아무 키나 눌러 계속하세요",
                28,
                new Vector2(0.2f, 0.08f),
                new Vector2(0.8f, 0.18f),
                TextAnchor.MiddleCenter);
            prompt.color = new Color(1f, 1f, 1f, 0.9f);
            prompt.raycastTarget = false;
            prompt.gameObject.SetActive(false);
            sequence.Configure(
                artwork,
                group,
                militaryImages,
                badRadioImages,
                caption,
                prompt);
            background.gameObject.SetActive(false);
            return sequence;
        }

        private static JoinEndingSequence CreateJoinEndingSequence(
            Transform canvas)
        {
            Image background = CreatePanel(
                canvas,
                "JoinEndingSequence",
                Vector2.zero,
                Vector2.one,
                Color.black);
            background.raycastTarget = true;

            CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.interactable = true;

            GameObject artworkObject = new("JoinEndingArtwork");
            artworkObject.transform.SetParent(background.transform, false);
            RectTransform artworkRect = artworkObject.AddComponent<RectTransform>();
            artworkRect.anchorMin = Vector2.zero;
            artworkRect.anchorMax = Vector2.one;
            artworkRect.offsetMin = Vector2.zero;
            artworkRect.offsetMax = Vector2.zero;
            RawImage artwork = artworkObject.AddComponent<RawImage>();
            artwork.raycastTarget = false;
            AspectRatioFitter fitter = artworkObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1672f / 941f;

            Texture[] survivorImages =
            {
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/join s/join s1.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/join s/join s2.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/join s/join s3.png")
            };
            Texture[] guardImages =
            {
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/join g/join g1.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/join g/join g2.png"),
                AssetDatabase.LoadAssetAtPath<Texture>(
                    $"{LastBunkerEndingRoot}/join g/join g3.png")
            };

            JoinEndingSequence sequence =
                background.gameObject.AddComponent<JoinEndingSequence>();
            Text caption = CreateEndingCaption(background.transform);
            Text prompt = CreateText(
                background.transform,
                "ContinuePrompt",
                "아무 키나 눌러 계속하세요",
                28,
                new Vector2(0.2f, 0.08f),
                new Vector2(0.8f, 0.18f),
                TextAnchor.MiddleCenter);
            prompt.color = new Color(1f, 1f, 1f, 0.9f);
            prompt.raycastTarget = false;
            prompt.gameObject.SetActive(false);
            sequence.Configure(
                artwork,
                group,
                survivorImages,
                guardImages,
                caption,
                prompt);
            background.gameObject.SetActive(false);
            return sequence;
        }

        private static Text CreateEndingCaption(Transform parent)
        {
            Text caption = CreateText(
                parent,
                "EndingCaption",
                string.Empty,
                34,
                new Vector2(0.08f, 0.08f),
                new Vector2(0.92f, 0.22f),
                TextAnchor.MiddleCenter);
            caption.fontStyle = FontStyle.Bold;
            caption.color = Color.white;
            caption.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            caption.verticalOverflow =
                VerticalWrapMode.Overflow;
            caption.raycastTarget = false;

            Outline outline = caption.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.96f);
            outline.effectDistance = new Vector2(3f, -3f);
            caption.gameObject.SetActive(false);
            return caption;
        }

        private static Button CreateTemporaryLastBunkerEndingButton(Transform canvas)
        {
            Button button = CreateButton(
                canvas,
                "TempLastBunkerEndingButton",
                "[임시] 엔딩 전날 밤",
                new Vector2(0.445f, 0.91f),
                new Vector2(0.65f, 0.975f),
                new Color(0.58f, 0.12f, 0.10f, 0.96f));
            button.gameObject.AddComponent<LastBunkerEndingDebugButton>();
            button.transform.SetAsLastSibling();
            return button;
        }

        private static void CreateTemporaryQuarter3EndingEveButtons(
            Transform canvas)
        {
            Button signalButton = CreateButton(
                canvas,
                "TempSignalEndingEveButton",
                "[임시] 신호계열 엔딩 전날 밤",
                new Vector2(0.66f, 0.91f),
                new Vector2(0.90f, 0.975f),
                new Color(0.12f, 0.28f, 0.48f, 0.96f));
            signalButton.gameObject
                .AddComponent<Quarter3EndingEveDebugButton>()
                .Configure(BranchRoute.Signal);
            signalButton.transform.SetAsLastSibling();

            Button joinButton = CreateButton(
                canvas,
                "TempJoinEndingEveButton",
                "[임시] 합류계열 엔딩 전날 밤",
                new Vector2(0.66f, 0.835f),
                new Vector2(0.90f, 0.90f),
                new Color(0.20f, 0.38f, 0.24f, 0.96f));
            joinButton.gameObject
                .AddComponent<Quarter3EndingEveDebugButton>()
                .Configure(BranchRoute.Join);
            joinButton.transform.SetAsLastSibling();
        }

        private static LastSurvivalRecordEndingSequence
            CreateLastSurvivalRecordEndingSequence(Transform canvas)
        {
            Image screen = CreatePanel(
                canvas,
                "LastSurvivalRecordEndingSequence",
                Vector2.zero,
                Vector2.one,
                Color.black);
            CanvasGroup group = screen.gameObject.AddComponent<CanvasGroup>();
            GameObject artworkObject = new("DeathArtwork");
            artworkObject.transform.SetParent(screen.transform, false);
            RectTransform artworkRect = artworkObject.AddComponent<RectTransform>();
            artworkRect.anchorMin = Vector2.zero;
            artworkRect.anchorMax = Vector2.one;
            artworkRect.offsetMin = Vector2.zero;
            artworkRect.offsetMax = Vector2.zero;
            RawImage artwork = artworkObject.AddComponent<RawImage>();
            artwork.raycastTarget = false;
            AspectRatioFitter fitter = artworkObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1672f / 941f;

            Texture starvation = AssetDatabase.LoadAssetAtPath<Texture>(
                $"{LastBunkerEndingRoot}/hungry poto.png");
            Texture dehydration = AssetDatabase.LoadAssetAtPath<Texture>(
                $"{LastBunkerEndingRoot}/water poto.png");
            LastSurvivalRecordEndingSequence sequence =
                screen.gameObject.AddComponent<LastSurvivalRecordEndingSequence>();
            sequence.Configure(
                screen, artwork, group, starvation, dehydration);
            screen.gameObject.SetActive(false);
            return sequence;
        }

        private static Button CreateTemporaryLastSurvivalRecordButton(
            Transform canvas)
        {
            Button button = CreateButton(
                canvas,
                "TempLastSurvivalRecordButton",
                "[임시] 굶주림 전날 밤",
                new Vector2(0.445f, 0.835f),
                new Vector2(0.65f, 0.90f),
                new Color(0.38f, 0.10f, 0.16f, 0.96f));
            button.gameObject.AddComponent<LastSurvivalRecordDebugButton>();
            button.transform.SetAsLastSibling();
            return button;
        }

        private static Button CreateTemporaryDehydrationEndingButton(
            Transform canvas)
        {
            Button button = CreateButton(
                canvas,
                "TempDehydrationEndingButton",
                "[임시] 탈수 전날 밤",
                new Vector2(0.445f, 0.76f),
                new Vector2(0.65f, 0.825f),
                new Color(0.10f, 0.24f, 0.42f, 0.96f));
            button.gameObject.AddComponent<
                LastSurvivalRecordDehydrationDebugButton>();
            button.transform.SetAsLastSibling();
            return button;
        }

        private static void CreateShelterInterface(Transform canvas,
            out Text dayText, out Text bunkerText, out Text statusText, out Text resourceText,
            out Text promptText, out Text messageText, out Dropdown ration, out Button endDay,
            out GameObject explorationPanel, out Text explorationResult, out Toggle gearToggle,
            out Button[] locationButtons, out Text[] locationDetails, out Button closeExploration,
            out GameObject storagePanel, out Text storageText, out Button closeStorage,
            out GameObject workPanel, out Slider workSlider, out Text workLabel,
            out GameObject endingPanel, out Text endingTitle, out Text endingDetail)
        {
            dayText = null;
            bunkerText = null;
            statusText = null;
            resourceText = null;

            Image bottom = CreatePanel(canvas, "BottomControlBar", Vector2.zero, new Vector2(1, 0.14f), new Color(0.025f, 0.04f, 0.045f, 0.95f));
            promptText = CreateText(bottom.transform, "InteractionPrompt", string.Empty, 23, new Vector2(0.2f, 0.5f), new Vector2(0.8f, 1), TextAnchor.MiddleCenter);
            messageText = CreateText(bottom.transform, "EventMessage", string.Empty, 20, new Vector2(0.18f, 0), new Vector2(0.82f, 0.5f), TextAnchor.MiddleCenter);
            ration = null;
            endDay = CreateButton(bottom.transform, "EndDayButton", "하루 종료", new Vector2(0.83f, 0.19f), new Vector2(0.99f, 0.82f), ColorFromHex("8D4039"));

            Object.DestroyImmediate(bottom.gameObject);
            RectTransform subtitles = CreateGroup(canvas, "ShelterSubtitles", new Vector2(0.16f, 0.025f), new Vector2(0.84f, 0.18f));
            promptText = CreateText(subtitles, "InteractionSubtitle", string.Empty, 25, new Vector2(0f, 0.50f), new Vector2(1f, 1f), TextAnchor.MiddleCenter);
            messageText = CreateText(subtitles, "InformationSubtitle", string.Empty, 22, new Vector2(0f, 0f), new Vector2(1f, 0.50f), TextAnchor.MiddleCenter);
            AddSubtitleOutline(promptText);
            AddSubtitleOutline(messageText);
            ration = null;
            endDay = null;

            explorationPanel = CreatePanel(canvas, "ExplorationPanel", new Vector2(0.2f, 0.16f), new Vector2(0.8f, 0.86f), new Color(0.025f, 0.045f, 0.05f, 0.985f)).gameObject;
            CreateText(explorationPanel.transform, "Title", "외부 탐사", 38, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), TextAnchor.MiddleCenter);
            explorationResult = CreateText(explorationPanel.transform, "ExplorationResult", "탐사 장소를 선택하세요.", 21, new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.84f), TextAnchor.MiddleCenter);
            locationButtons = new Button[3]; locationDetails = new Text[3];
            string[] cardNames = { "ConvenienceStoreCard", "HospitalCard", "HardwareStoreCard" };
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.04f + i * 0.32f; float x1 = x0 + 0.29f;
                Image card = CreatePanel(explorationPanel.transform, cardNames[i], new Vector2(x0, 0.27f), new Vector2(x1, 0.68f), new Color(0.1f, 0.14f, 0.15f, 0.95f));
                locationDetails[i] = CreateText(card.transform, "LocationDetails", string.Empty, 18, new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.96f), TextAnchor.UpperLeft);
                locationButtons[i] = CreateButton(card.transform, "DepartButton", "출발", new Vector2(0.18f, 0.03f), new Vector2(0.82f, 0.2f), ColorFromHex("426A65"));
            }
            gearToggle = CreateToggle(explorationPanel.transform, "GearToggle", "장비 사용", new Vector2(0.25f, 0.15f), new Vector2(0.52f, 0.24f));
            closeExploration = CreateButton(explorationPanel.transform, "CancelButton", "취소", new Vector2(0.57f, 0.15f), new Vector2(0.76f, 0.24f), ColorFromHex("4A565C"));

            storagePanel = CreatePanel(canvas, "StorageInventoryPanel", new Vector2(0.33f, 0.25f), new Vector2(0.67f, 0.77f), new Color(0.025f, 0.045f, 0.05f, 0.99f)).gameObject;
            CreateText(storagePanel.transform, "StorageTitle", "창고 물자", 36, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.96f), TextAnchor.MiddleCenter);
            storageText = CreateText(storagePanel.transform, "StorageResources", string.Empty, 26, new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.8f), TextAnchor.UpperLeft);
            closeStorage = CreateButton(storagePanel.transform, "StorageCloseButton", "닫기", new Vector2(0.32f, 0.06f), new Vector2(0.68f, 0.17f), ColorFromHex("4A565C"));

            workPanel = CreatePanel(canvas, "WorkProgressPanel", new Vector2(0.34f, 0.45f), new Vector2(0.66f, 0.6f), new Color(0.025f, 0.045f, 0.05f, 0.98f)).gameObject;
            workLabel = CreateText(workPanel.transform, "WorkLabel", "작업 중...", 22, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f), TextAnchor.MiddleCenter);
            workSlider = CreateSlider(workPanel.transform, "WorkProgress", new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.48f));

            endingPanel = CreatePanel(canvas, "EndingPanel", new Vector2(0.25f, 0.24f), new Vector2(0.75f, 0.76f), new Color(0.015f, 0.025f, 0.03f, 0.995f)).gameObject;
            endingTitle = CreateText(endingPanel.transform, "EndingTitle", "게임 종료", 52, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter);
            endingDetail = CreateText(endingPanel.transform, "EndingDetail", string.Empty, 28, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.62f), TextAnchor.MiddleCenter);
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>(); image.color = color; return image;
        }

        private static RectTransform CreateGroup(Transform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject go = new(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 min, Vector2 max, TextAnchor alignment)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = new Vector2(12, 8); rect.offsetMax = new Vector2(-12, -8);
            Text text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.text = value; text.alignment = alignment;
            text.color = ColorFromHex("E6E0D1"); text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }

        private static void AddSubtitleOutline(Text text)
        {
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            text.raycastTarget = false;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Color color)
        {
            Image image = CreatePanel(parent, name, min, max, color); Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            CreateText(image.transform, "Label", label, 22, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter); return button;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, Vector2 min, Vector2 max, string[] options)
        {
            GameObject go = DefaultControls.CreateDropdown(new DefaultControls.Resources()); go.name = name; go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Dropdown dropdown = go.GetComponent<Dropdown>(); dropdown.ClearOptions(); dropdown.AddOptions(new List<string>(options));
            foreach (Text text in go.GetComponentsInChildren<Text>(true)) { text.font = font; text.fontSize = 18; }
            return dropdown;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            GameObject go = DefaultControls.CreateToggle(new DefaultControls.Resources()); go.name = name; go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Text text = go.GetComponentInChildren<Text>(); text.text = label; text.font = font; text.fontSize = 20; text.color = Color.white; return go.GetComponent<Toggle>();
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources()); go.name = name; go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Slider slider = go.GetComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0f; return slider;
        }

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color;
        }

        private static void SaveScene(Scene scene, string name)
        {
            string path = $"{SceneRoot}/{name}.unity"; EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, path);
        }
    }
}
#endif
