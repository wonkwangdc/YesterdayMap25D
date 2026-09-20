using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

namespace YesterdayMap.Editor
{
    /// <summary>
    /// Creates persistent low-poly mesh assets and LOD groups for the existing
    /// Scavenge furniture and loot while preserving source FBXs and colliders.
    /// </summary>
    public static class ScavengeMeshLodOptimizer
    {
        private const string ScavengeScenePath = "Assets/_Project/Scenes/Scavenge.unity";
        private const string ShelterScenePath = "Assets/_Project/Scenes/Shelter.unity";
        private const string ScavengeOutputFolder = "Assets/_Project/Art/Optimized/ScavengeLOD";
        private const string ShelterOutputFolder = "Assets/_Project/Art/Optimized/ShelterLOD";
        private const int MinimumTriangleCount = 100000;
        private const float Lod1Quality = 0.30f;
        private const float Lod2Quality = 0.10f;

        private static readonly string[] ScavengeTargetRoots =
        {
            "EditableFurniture",
            "ScavengePropLoot"
        };

        private static readonly string[] ShelterTargetRoots =
        {
            "Barricade",
            "Bed",
            "WaterPurifier",
            "Generator",
            "Radio",
            "Storage",
            "Workbench",
            "ExitDoor",
            "DrainagePump",
            "MeshyShelterBunker",
            "BunkerObjects",
            "BunkerFurniture",
            "CollectedSpecialItems"
        };

        [MenuItem("Yesterday Map/Optimize Scavenge Mesh LODs (Preserve Scene)")]
        public static void Optimize()
        {
            OptimizeScene(
                ScavengeScenePath,
                ScavengeOutputFolder,
                ScavengeTargetRoots,
                "Scavenge");
        }

        [MenuItem("Yesterday Map/Optimize Shelter Mesh LODs (Preserve Scene)")]
        public static void OptimizeShelter()
        {
            OptimizeScene(
                ShelterScenePath,
                ShelterOutputFolder,
                ShelterTargetRoots,
                "Shelter");
        }

        /// <summary>
        /// Opens the latest Shelter scene before applying LODs in batch mode.
        /// </summary>
        public static void OptimizeShelterBatch()
        {
            EditorSceneManager.OpenScene(ShelterScenePath, OpenSceneMode.Single);
            OptimizeShelter();
        }

        private static void OptimizeScene(
            string expectedScenePath,
            string outputFolder,
            string[] targetRoots,
            string sceneLabel)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"Yesterday Map: Stop Play Mode before optimizing {sceneLabel} meshes.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != expectedScenePath)
            {
                Debug.LogWarning($"Yesterday Map: Open {sceneLabel}.unity before running mesh optimization.");
                return;
            }

            EnsureOutputFolder(outputFolder);

            Dictionary<string, Mesh> generatedMeshes = new();
            int optimizedRenderers = 0;
            long sourceTriangles = 0;
            long lod1Triangles = 0;
            long lod2Triangles = 0;

            try
            {
                foreach (string rootName in targetRoots)
                {
                    GameObject root = FindRoot(scene, rootName);
                    if (root == null)
                    {
                        Debug.LogWarning($"Yesterday Map: Scavenge optimization root '{rootName}' was not found.");
                        continue;
                    }

                    MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
                    foreach (MeshFilter filter in filters)
                    {
                        if (!TryOptimizeRenderer(
                                filter,
                                outputFolder,
                                generatedMeshes,
                                out long originalCount,
                                out long firstLodCount,
                                out long secondLodCount))
                            continue;

                        optimizedRenderers++;
                        sourceTriangles += originalCount;
                        lod1Triangles += firstLodCount;
                        lod2Triangles += secondLodCount;
                    }
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log(
                    $"Yesterday Map: optimized {optimizedRenderers} {sceneLabel} renderers. " +
                    $"Triangles LOD0/LOD1/LOD2 = {sourceTriangles:N0}/{lod1Triangles:N0}/{lod2Triangles:N0}. " +
                    "Source FBXs, prefab GUIDs, transforms, and colliders were preserved.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Yesterday Map/Optimize Scavenge Textures (Preserve Assets)")]
        public static void OptimizeTextures()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Yesterday Map: Stop Play Mode before optimizing Scavenge textures.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScavengeScenePath)
            {
                Debug.LogWarning("Yesterday Map: Open Scavenge.unity before optimizing textures.");
                return;
            }

            HashSet<string> texturePaths = CollectTexturePaths(scene);
            int changed = 0;
            foreach (string path in texturePaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                string lowerPath = path.ToLowerInvariant();
                bool isHouseTexture = lowerPath.Contains("/environment/meshyhouse/");
                bool isSmallLoot = lowerPath.Contains("/items/scavengeprops/");
                bool isSupportingMap =
                    lowerPath.Contains("normal") ||
                    lowerPath.Contains("metallic") ||
                    lowerPath.Contains("emission");
                int targetSize = !isHouseTexture && (isSmallLoot || isSupportingMap)
                    ? 1024
                    : Mathf.Min(importer.maxTextureSize, 2048);

                bool needsChange =
                    importer.maxTextureSize != targetSize ||
                    !importer.mipmapEnabled ||
                    !importer.streamingMipmaps ||
                    importer.isReadable;
                if (!needsChange)
                    continue;

                importer.maxTextureSize = targetSize;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                importer.isReadable = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                changed++;
            }

            Debug.Log(
                $"Yesterday Map: optimized {changed} of {texturePaths.Count} Scavenge textures. " +
                "House and primary furniture color textures remain up to 2K; small loot and supporting maps are capped at 1K.");
        }

        private static bool TryOptimizeRenderer(
            MeshFilter filter,
            string outputFolder,
            Dictionary<string, Mesh> generatedMeshes,
            out long sourceTriangles,
            out long lod1Triangles,
            out long lod2Triangles)
        {
            sourceTriangles = 0;
            lod1Triangles = 0;
            lod2Triangles = 0;

            if (filter == null || filter.sharedMesh == null)
                return false;

            MeshRenderer sourceRenderer = filter.GetComponent<MeshRenderer>();
            if (sourceRenderer == null || IsGeneratedLod(filter.gameObject))
                return false;

            sourceTriangles = CountTriangles(filter.sharedMesh);
            if (sourceTriangles < MinimumTriangleCount)
                return false;

            RemovePreviousLods(filter.gameObject);

            string sourceKey = GetSourceKey(filter.sharedMesh);
            Mesh lod1Mesh = GetOrCreateSimplifiedMesh(
                filter.sharedMesh,
                sourceKey,
                "LOD1",
                Lod1Quality,
                outputFolder,
                generatedMeshes);
            Mesh lod2Mesh = GetOrCreateSimplifiedMesh(
                filter.sharedMesh,
                sourceKey,
                "LOD2",
                Lod2Quality,
                outputFolder,
                generatedMeshes);

            if (lod1Mesh == null || lod2Mesh == null)
                return false;

            lod1Triangles = CountTriangles(lod1Mesh);
            lod2Triangles = CountTriangles(lod2Mesh);

            MeshRenderer lod1Renderer = CreateLodRenderer(
                filter.transform,
                "__OptimizedLOD1",
                lod1Mesh,
                sourceRenderer,
                true);
            MeshRenderer lod2Renderer = CreateLodRenderer(
                filter.transform,
                "__OptimizedLOD2",
                lod2Mesh,
                sourceRenderer,
                false);

            LODGroup lodGroup = filter.GetComponent<LODGroup>();
            if (lodGroup == null)
                lodGroup = filter.gameObject.AddComponent<LODGroup>();

            lodGroup.fadeMode = LODFadeMode.None;
            lodGroup.animateCrossFading = false;
            lodGroup.SetLODs(new[]
            {
                new LOD(0.45f, new Renderer[] { sourceRenderer }),
                new LOD(0.16f, new Renderer[] { lod1Renderer }),
                new LOD(0.03f, new Renderer[] { lod2Renderer })
            });
            lodGroup.RecalculateBounds();
            EditorUtility.SetDirty(lodGroup);
            return true;
        }

        private static Mesh GetOrCreateSimplifiedMesh(
            Mesh source,
            string sourceKey,
            string lodName,
            float quality,
            string outputFolder,
            Dictionary<string, Mesh> cache)
        {
            string cacheKey = $"{sourceKey}_{lodName}";
            if (cache.TryGetValue(cacheKey, out Mesh cached))
                return cached;

            string sourceFile = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source));
            string meshName = SanitizeFileName(source.name);
            string assetPath = $"{outputFolder}/{sourceFile}_{meshName}_{sourceKey}_{lodName}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                cache[cacheKey] = existing;
                return existing;
            }

            EditorUtility.DisplayProgressBar(
                "Scavenge mesh optimization",
                $"Creating {source.name} {lodName}",
                quality);

            MeshSimplifier simplifier = new(source)
            {
                SimplificationOptions = SimplificationOptions.Default
            };
            simplifier.SimplifyMesh(quality);
            Mesh simplified = simplifier.ToMesh();
            simplified.name = $"{source.name}_{lodName}";
            simplified.RecalculateBounds();
            AssetDatabase.CreateAsset(simplified, assetPath);
            cache[cacheKey] = simplified;
            return simplified;
        }

        private static MeshRenderer CreateLodRenderer(
            Transform parent,
            string name,
            Mesh mesh,
            MeshRenderer source,
            bool castShadows)
        {
            GameObject owner = new(name);
            owner.transform.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(
                owner,
                GameObjectUtility.GetStaticEditorFlags(source.gameObject));

            MeshFilter filter = owner.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = owner.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = castShadows
                ? source.shadowCastingMode
                : ShadowCastingMode.Off;
            renderer.receiveShadows = source.receiveShadows;
            renderer.lightProbeUsage = source.lightProbeUsage;
            renderer.reflectionProbeUsage = source.reflectionProbeUsage;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        private static void RemovePreviousLods(GameObject source)
        {
            for (int index = source.transform.childCount - 1; index >= 0; index--)
            {
                Transform child = source.transform.GetChild(index);
                if (IsGeneratedLod(child.gameObject))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            LODGroup existingGroup = source.GetComponent<LODGroup>();
            if (existingGroup == null)
                return;

            bool ownedByOptimizer = false;
            foreach (LOD lod in existingGroup.GetLODs())
            {
                foreach (Renderer renderer in lod.renderers)
                {
                    if (renderer != null && IsGeneratedLod(renderer.gameObject))
                    {
                        ownedByOptimizer = true;
                        break;
                    }
                }
            }

            if (ownedByOptimizer)
                UnityEngine.Object.DestroyImmediate(existingGroup);
        }

        private static bool IsGeneratedLod(GameObject owner)
        {
            return owner != null && owner.name.StartsWith("__OptimizedLOD", StringComparison.Ordinal);
        }

        private static string GetSourceKey(Mesh mesh)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId))
                return $"{guid.Substring(0, 8)}_{localId}";

            return Mathf.Abs(mesh.GetInstanceID()).ToString();
        }

        private static long CountTriangles(Mesh mesh)
        {
            long triangles = 0;
            for (int index = 0; index < mesh.subMeshCount; index++)
                triangles += (long)mesh.GetIndexCount(index) / 3;
            return triangles;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }

            return null;
        }

        private static HashSet<string> CollectTexturePaths(Scene scene)
        {
            HashSet<string> texturePaths = new(StringComparer.OrdinalIgnoreCase);
            string[] roots = { "EditableFurniture", "ScavengePropLoot", "HouseEnvironment" };
            foreach (string rootName in roots)
            {
                GameObject root = FindRoot(scene, rootName);
                if (root == null)
                    continue;

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null)
                            continue;

                        foreach (string propertyName in material.GetTexturePropertyNames())
                        {
                            Texture texture = material.GetTexture(propertyName);
                            if (texture == null)
                                continue;

                            string path = AssetDatabase.GetAssetPath(texture);
                            if (!string.IsNullOrEmpty(path))
                                texturePaths.Add(path);
                        }
                    }
                }
            }

            return texturePaths;
        }

        private static void EnsureOutputFolder(string outputFolder)
        {
            const string optimizedRoot = "Assets/_Project/Art/Optimized";
            if (!AssetDatabase.IsValidFolder(optimizedRoot))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Optimized");
            if (!AssetDatabase.IsValidFolder(outputFolder))
                AssetDatabase.CreateFolder(optimizedRoot, Path.GetFileName(outputFolder));
        }
    }
}
