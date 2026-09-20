using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayMap.EditorTools
{
    /// <summary>
    /// Rebuilds the NewHome furniture meshes from the untouched Meshy FBX files.
    /// Existing standalone mesh assets are overwritten in-place so every prefab
    /// and scene keeps its GUID, transform, collider and placement.
    /// </summary>
    public static class NewHomeFurnitureOriginalRestorer
    {
        private const string ArtRoot = "Assets/_Project/Art/Furniture/NewHome";
        private const string MaterialRoot = "Assets/_Project/Materials/NewHome";
        private const string PrefabRoot = "Assets/_Project/Prefabs/Furniture/NewHome";

        private static readonly string[] FurnitureNames =
        {
            "BedsideLamp",
            "CleaningTools",
            "DracaenaPlant",
            "HarvestDiningSet",
            "IvorySofa",
            "MarbleKitchenSet",
            "MediaConsole",
            "OakDiningTable",
            "OakKitchenSet",
            "OakVanity",
            "SageBed",
            "SilverFridge",
            "TablePlant",
            "UtilityShelves",
            "WhiteToilet"
        };

        [MenuItem("Yesterday Map/Art/Restore New Home Furniture From Original FBX")]
        public static void RestoreAll()
        {
            try
            {
                ConfigureSourceModels();
                ConfigureTextures();
                ReplaceExistingFurnitureMeshes();
                CreateOakEntranceAssets();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[FurnitureOriginalRestorer] 완료: 원본 가구 15종 복구 + OakEntrance 원본 프리팹 생성");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ConfigureSourceModels()
        {
            string[] names = FurnitureNames.Concat(new[] { "OakEntrance" }).ToArray();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                string path = SourceFbxPath(name);
                EditorUtility.DisplayProgressBar("원본 FBX 설정", name, (float)i / names.Length);

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    throw new InvalidOperationException($"FBX 임포터를 찾지 못했습니다: {path}");

                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.isReadable = true;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.Import;
                importer.weldVertices = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureTextures()
        {
            string[] names = FurnitureNames.Concat(new[] { "OakEntrance" }).ToArray();
            string[] suffixes = { "BaseColor", "Metallic", "Normal", "Roughness" };

            int total = names.Length * suffixes.Length;
            int current = 0;
            foreach (string name in names)
            {
                foreach (string suffix in suffixes)
                {
                    EditorUtility.DisplayProgressBar("4K 텍스처 설정", $"{name} {suffix}", (float)current++ / total);
                    string path = $"{ArtRoot}/{name}/{name}_{suffix}.png";
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                        throw new InvalidOperationException($"텍스처 임포터를 찾지 못했습니다: {path}");

                    bool isNormal = suffix == "Normal";
                    bool isColor = suffix == "BaseColor";
                    importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    importer.sRGBTexture = isColor;
                    importer.maxTextureSize = 4096;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.compressionQuality = 100;
                    importer.mipmapEnabled = true;
                    importer.streamingMipmaps = true;
                    importer.anisoLevel = 4;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ReplaceExistingFurnitureMeshes()
        {
            for (int i = 0; i < FurnitureNames.Length; i++)
            {
                string name = FurnitureNames[i];
                EditorUtility.DisplayProgressBar("원본 메시 교체", name, (float)i / FurnitureNames.Length);

                Mesh source = LoadLargestMesh(SourceFbxPath(name));
                string targetPath = $"{ArtRoot}/{name}/{name}_Optimized.asset";
                Mesh target = AssetDatabase.LoadAssetAtPath<Mesh>(targetPath);
                if (target == null)
                    throw new InvalidOperationException($"교체 대상 메시를 찾지 못했습니다: {targetPath}");

                long oldTriangles = TriangleCount(target);
                long newTriangles = TriangleCount(source);
                EditorUtility.CopySerialized(source, target);
                target.name = name + "_Optimized";
                EditorUtility.SetDirty(target);
                Debug.Log($"[FurnitureOriginalRestorer] {name}: {oldTriangles:N0} -> {newTriangles:N0} triangles");
            }
        }

        private static void CreateOakEntranceAssets()
        {
            const string name = "OakEntrance";
            Mesh source = LoadLargestMesh(SourceFbxPath(name));
            string meshPath = $"{ArtRoot}/{name}/{name}_Optimized.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            EditorUtility.CopySerialized(source, mesh);
            mesh.name = name + "_Optimized";
            EditorUtility.SetDirty(mesh);

            Shader shader = Shader.Find("YesterdayMap/Environment/MeshyFurniturePBR") ?? Shader.Find("Standard");
            string materialPath = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtRoot}/{name}/{name}_BaseColor.png"));
            material.SetTexture("_MetallicTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtRoot}/{name}/{name}_Metallic.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtRoot}/{name}/{name}_Normal.png"));
            material.SetTexture("_RoughnessTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtRoot}/{name}/{name}_Roughness.png"));
            material.SetColor("_Color", Color.white);
            material.SetFloat("_NormalStrength", 0.85f);
            material.SetFloat("_MetallicStrength", 0.8f);
            material.SetFloat("_SmoothnessStrength", 0.72f);
            EditorUtility.SetDirty(material);

            GameObject root = new GameObject(name);
            GameObject model = new GameObject("Model");
            try
            {
                model.transform.SetParent(root.transform, false);
                model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                float sourceHeight = Mathf.Max(mesh.bounds.size.z, 0.000001f);
                float scale = 2.2f / sourceHeight;
                model.transform.localScale = Vector3.one * scale;

                MeshFilter filter = model.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = model.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;

                Bounds firstBounds = CalculateRootLocalBounds(mesh.bounds, model.transform.localToWorldMatrix);
                model.transform.localPosition = new Vector3(0f, -firstBounds.min.y, 0f);
                Bounds finalBounds = CalculateRootLocalBounds(mesh.bounds, model.transform.localToWorldMatrix);

                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = finalBounds.center;
                collider.size = finalBounds.size;

                GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
                GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{name}.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            Debug.Log($"[FurnitureOriginalRestorer] OakEntrance: {TriangleCount(mesh):N0} triangles, 4K PBR prefab 생성");
        }

        private static Mesh LoadLargestMesh(string path)
        {
            Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .OrderByDescending(TriangleCount)
                .FirstOrDefault();

            if (mesh == null)
                throw new InvalidOperationException($"FBX 안에서 메시를 찾지 못했습니다: {path}");
            return mesh;
        }

        private static long TriangleCount(Mesh mesh)
        {
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                count += (long)mesh.GetIndexCount(i) / 3L;
            return count;
        }

        private static Bounds CalculateRootLocalBounds(Bounds meshBounds, Matrix4x4 localToWorld)
        {
            Vector3 center = meshBounds.center;
            Vector3 extents = meshBounds.extents;
            Bounds result = new Bounds(localToWorld.MultiplyPoint3x4(center), Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                result.Encapsulate(localToWorld.MultiplyPoint3x4(corner));
            }
            return result;
        }

        private static string SourceFbxPath(string name) => $"{ArtRoot}/{name}/{name}_Original.fbx";
    }
}
