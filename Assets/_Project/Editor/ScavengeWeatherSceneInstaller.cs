#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using YesterdayMap.Scavenge;
using Object = UnityEngine.Object;

namespace YesterdayMap.Editor
{
    /// <summary>
    /// Installs the persistent weather objects into the existing Scavenge scene.
    /// Re-running the menu only replaces the ScavengeWeather hierarchy.
    /// </summary>
    public static class ScavengeWeatherSceneInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scavenge.unity";
        private const string MaterialFolder = "Assets/_Project/Materials/Weather";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Environment/Weather";
        private const string PrefabPath = PrefabFolder + "/ScavengeWeather.prefab";
        private const string TexturePath = MaterialFolder + "/RainStreakTexture.asset";
        private const string MaterialPath = MaterialFolder + "/RainStreak.mat";
        private const string RainEmissionMeshPath =
            MaterialFolder + "/WindowRainEmissionMesh.asset";
        private const string ThunderPath = "Assets/_Project/Resources/Audio/SFX/Weather_Thunder.mp3";
        private const string RainLoopPath = "Assets/_Project/Resources/Audio/SFX/Weather_RainLoop.wav";

        [MenuItem("Yesterday Map/Install Scavenge Weather")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Yesterday Map: Stop Play Mode before installing Scavenge weather.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                EnsureTemporaryRainLoop();
                Camera camera = FindInScene<Camera>(scene, "MainCamera");
                GameObject house = FindRoot(scene, "HouseEnvironment");
                AudioClip thunder = AssetDatabase.LoadAssetAtPath<AudioClip>(ThunderPath);
                AudioClip rainLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(RainLoopPath);
                if (camera == null || house == null || thunder == null || rainLoop == null)
                {
                    Debug.LogError(
                        "Yesterday Map: MainCamera, HouseEnvironment, or weather audio is missing.");
                    return;
                }

                Transform[] windows = FindWindowRoots(house.transform);
                if (windows.Length == 0)
                {
                    Debug.LogError("Yesterday Map: No InteriorWindows were found for window rain.");
                    return;
                }

                GameObject existing = FindRoot(scene, "ScavengeWeather");
                if (existing != null)
                    Object.DestroyImmediate(existing);

                Material rainMaterial = GetOrCreateRainMaterial();
                Mesh rainEmissionMesh = GetOrCreateRainEmissionMesh(windows);
                GameObject root = new("ScavengeWeather");
                root.transform.position = Vector3.zero;

                ParticleSystem windowRain = CreateWindowRain(
                    root.transform,
                    rainMaterial,
                    windows,
                    rainEmissionMesh);
                Light flash = CreateLightning(root.transform);
                AudioSource rainAudio = CreateRainAudio(root.transform, rainLoop);
                AudioSource thunderAudio = CreateThunderAudio(root.transform);

                ScavengeWeatherController controller = root.AddComponent<ScavengeWeatherController>();
                controller.Configure(
                    null,
                    windowRain,
                    flash,
                    rainAudio,
                    rainLoop,
                    thunderAudio,
                    thunder,
                    false,
                    Array.Empty<ParticleSystem>());

                EnsureFolder("Assets/_Project/Prefabs/Environment", "Weather");
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
                if (prefab == null)
                    throw new System.InvalidOperationException("Failed to save Scavenge weather prefab.");

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "ScavengeWeather";
                instance.transform.position = Vector3.zero;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"Yesterday Map: Window rain and lightning installed " +
                    $"for {windows.Length} windows.");
            }
            finally
            {
                if (openedTemporarily && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static ParticleSystem CreateWindowRain(
            Transform parent,
            Material material,
            Transform[] windows,
            Mesh emissionMesh)
        {
            float totalArea = 0f;
            float maximumHeight = 1.2f;
            foreach (Transform window in windows)
            {
                Bounds bounds = CalculateBounds(window.gameObject);
                float width = Mathf.Max(0.8f, ProjectBoundsSize(bounds, window.right));
                float height = Mathf.Max(1.2f, bounds.size.y);
                totalArea += width * height;
                maximumHeight = Mathf.Max(maximumHeight, height);
            }

            float emissionRate = Mathf.Clamp(totalArea * 20f, 350f, 1100f);
            float fallTime = Mathf.Sqrt(
                2f * maximumHeight / (Physics.gravity.magnitude * 3f));

            GameObject owner = new("WindowRain");
            owner.transform.SetParent(parent, false);

            ParticleSystem rain = owner.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                fallTime * 0.9f,
                fallTime * 1.08f);
            main.startSpeed = 0f;
            main.gravityModifier = 3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.034f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.62f, 0.76f, 0.9f, 0.22f),
                new Color(0.78f, 0.88f, 1f, 0.42f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(emissionRate * fallTime * 1.25f);

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            ParticleSystem.ShapeModule shape = rain.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.mesh = emissionMesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.useMeshColors = false;

            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.enabled = false;

            ParticleSystem.NoiseModule noise = rain.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.15f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            ParticleSystemRenderer renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 0.7f;
            renderer.cameraVelocityScale = 0f;
            renderer.material = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.sortingFudge = 2f;

            rain.Play();
            return rain;
        }

        private static float ProjectBoundsSize(Bounds bounds, Vector3 direction)
        {
            Vector3 axis = direction.normalized;
            Vector3 size = bounds.size;
            return Mathf.Abs(axis.x) * size.x +
                   Mathf.Abs(axis.y) * size.y +
                   Mathf.Abs(axis.z) * size.z;
        }

        private static Mesh GetOrCreateRainEmissionMesh(Transform[] windows)
        {
            List<Vector3> vertices = new(windows.Length * 4);
            List<int> triangles = new(windows.Length * 6);

            foreach (Transform window in windows)
            {
                Bounds bounds = CalculateBounds(window.gameObject);
                Vector3 right = window.right.normalized;
                Vector3 outward = -window.forward.normalized;
                float halfWidth =
                    Mathf.Max(0.4f, ProjectBoundsSize(bounds, right) * 0.45f);
                const float halfDepth = 0.1f;
                Vector3 center =
                    new Vector3(bounds.center.x, bounds.max.y + 0.12f, bounds.center.z) +
                    outward * 0.28f;
                int first = vertices.Count;

                vertices.Add(center - right * halfWidth - outward * halfDepth);
                vertices.Add(center + right * halfWidth - outward * halfDepth);
                vertices.Add(center + right * halfWidth + outward * halfDepth);
                vertices.Add(center - right * halfWidth + outward * halfDepth);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RainEmissionMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "WindowRainEmissionMesh" };
                AssetDatabase.CreateAsset(mesh, RainEmissionMeshPath);
            }
            else
            {
                mesh.Clear();
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            Bounds bounds = new(root.transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            foreach (Collider collider in colliders)
            {
                if (collider == null) continue;
                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (!hasBounds)
                throw new System.InvalidOperationException("HouseEnvironment has no renderer or collider bounds.");

            return bounds;
        }

        private static Light CreateLightning(Transform parent)
        {
            GameObject owner = new("LightningFlash");
            owner.transform.SetParent(parent, false);
            owner.transform.localRotation = Quaternion.Euler(50f, -35f, 0f);

            Light light = owner.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.66f, 0.8f, 1f);
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        private static AudioSource CreateThunderAudio(Transform parent)
        {
            GameObject owner = new("ThunderAudio");
            owner.transform.SetParent(parent, false);

            AudioSource source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            return source;
        }

        private static AudioSource CreateRainAudio(Transform parent, AudioClip rainLoop)
        {
            GameObject owner = new("RainAudio");
            owner.transform.SetParent(parent, false);

            AudioSource source = owner.AddComponent<AudioSource>();
            source.clip = rainLoop;
            source.playOnAwake = true;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.24f;
            return source;
        }

        private static void EnsureTemporaryRainLoop()
        {
            if (File.Exists(RainLoopPath))
                return;

            const int sampleRate = 22050;
            const int channels = 2;
            const int durationSeconds = 12;
            int frameCount = sampleRate * durationSeconds;
            float[] samples = new float[frameCount * channels];
            System.Random random = new(250804);
            float[] lowPass = new float[channels];
            float[] midPass = new float[channels];
            float[] drop = new float[channels];

            for (int frame = 0; frame < frameCount; frame++)
            {
                for (int channel = 0; channel < channels; channel++)
                {
                    float white = (float)(random.NextDouble() * 2.0 - 1.0);
                    lowPass[channel] = lowPass[channel] * 0.985f + white * 0.015f;
                    midPass[channel] = midPass[channel] * 0.78f + white * 0.22f;
                    if (random.NextDouble() < 0.00055)
                        drop[channel] = Mathf.Lerp(0.25f, 0.7f, (float)random.NextDouble());

                    float droplet = drop[channel] * white;
                    drop[channel] *= 0.986f;
                    samples[frame * channels + channel] = Mathf.Clamp(
                        lowPass[channel] * 0.5f + midPass[channel] * 0.22f + droplet * 0.14f,
                        -0.82f,
                        0.82f);
                }
            }

            int crossfadeFrames = sampleRate;
            for (int frame = 0; frame < crossfadeFrames; frame++)
            {
                float blend = frame / (crossfadeFrames - 1f);
                int endFrame = frameCount - crossfadeFrames + frame;
                for (int channel = 0; channel < channels; channel++)
                {
                    int endIndex = endFrame * channels + channel;
                    int startIndex = frame * channels + channel;
                    samples[endIndex] = Mathf.Lerp(samples[endIndex], samples[startIndex], blend);
                }
            }

            WritePcm16Wav(RainLoopPath, samples, sampleRate, channels);
            AssetDatabase.ImportAsset(RainLoopPath, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(RainLoopPath) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.45f;
                settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }
        }

        private static void WritePcm16Wav(
            string assetPath,
            float[] samples,
            int sampleRate,
            int channels)
        {
            int dataLength = samples.Length * sizeof(short);
            using FileStream stream = File.Create(assetPath);
            using BinaryWriter writer = new(stream);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * sizeof(short));
            writer.Write((short)(channels * sizeof(short)));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            foreach (float sample in samples)
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }

        private static Material GetOrCreateRainMaterial()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Weather");

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                texture = new Texture2D(16, 64, TextureFormat.RGBA32, false)
                {
                    name = "RainStreakTexture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                for (int y = 0; y < texture.height; y++)
                {
                    float vertical = Mathf.Sin((y + 0.5f) / texture.height * Mathf.PI);
                    for (int x = 0; x < texture.width; x++)
                    {
                        float center = Mathf.Abs((x + 0.5f) / texture.width * 2f - 1f);
                        float horizontal = Mathf.Pow(1f - Mathf.Clamp01(center), 2.6f);
                        texture.SetPixel(
                            x,
                            y,
                            new Color(0.78f, 0.88f, 1f, horizontal * vertical * 0.9f));
                    }
                }

                texture.Apply();
                AssetDatabase.CreateAsset(texture, TexturePath);
            }

            bool usesScriptableRenderPipeline =
                GraphicsSettings.currentRenderPipeline != null ||
                GraphicsSettings.defaultRenderPipeline != null;
            string shaderName = usesScriptableRenderPipeline
                ? "Universal Render Pipeline/Particles/Unlit"
                : "Legacy Shaders/Particles/Alpha Blended Premultiply";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"Particle shader is unavailable: {shaderName}");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "RainStreak" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(0.62f, 0.76f, 0.9f, 0.45f));
            if (material.HasProperty("_TintColor"))
                material.SetColor("_TintColor", new Color(0.72f, 0.84f, 1f, 0.38f));
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform[] FindWindowRoots(Transform houseRoot)
        {
            Transform container = null;
            foreach (Transform candidate in houseRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "InteriorWindows")
                {
                    container = candidate;
                    break;
                }
            }

            if (container == null)
                return Array.Empty<Transform>();

            List<Transform> windows = new();
            foreach (Transform child in container)
            {
                if (child.GetComponentInChildren<Renderer>(true) != null)
                    windows.Add(child);
            }

            return windows.ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static T FindInScene<T>(Scene scene, string objectName) where T : Component
        {
            GameObject root = FindRoot(scene, objectName);
            return root != null ? root.GetComponent<T>() : null;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                    return root;
            }

            return null;
        }
    }
}
#endif
