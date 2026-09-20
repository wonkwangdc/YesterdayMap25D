using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayMap.Scavenge
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ScavengeScreenOutline : MonoBehaviour
    {
        private const string MaskShaderName = "Hidden/YesterdayMap/ScavengeMask";
        private const string CompositeShaderName = "Hidden/YesterdayMap/ScavengeOutlineComposite";
        private static ScavengeScreenOutline instance;

        [SerializeField, ColorUsage(false, true)]
        private Color outlineColor = new(1f, 0.96f, 0.78f, 1f);
        [SerializeField, Range(1f, 4f)] private float outlinePixels = 3f;

        private readonly HashSet<Renderer> outlinedRenderers = new();
        private Camera targetCamera;
        private Material maskMaterial;
        private Material compositeMaterial;
        private RenderTexture maskTexture;
        private CommandBuffer maskCommands;
        private bool commandBufferAttached;
        private bool commandsDirty = true;
        private int maskWidth;
        private int maskHeight;

        public static void Register(ScavengeCollectible collectible)
        {
            if (instance == null || collectible == null) return;
            instance.RegisterRenderers(collectible);
        }

        public static void Unregister(ScavengeCollectible collectible)
        {
            if (instance == null || collectible == null) return;
            Renderer[] renderers = collectible.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (instance.outlinedRenderers.Remove(renderer))
                {
                    instance.commandsDirty = true;
                }
            }
        }

        public static void UnregisterRenderers(Transform visualRoot)
        {
            if (instance == null || visualRoot == null) return;

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (instance.outlinedRenderers.Remove(renderer))
                {
                    instance.commandsDirty = true;
                }
            }
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            instance = this;
            CreateMaterials();
            RefreshCollectibles();
        }

        private void OnEnable()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            instance = this;
            commandsDirty = true;
        }

        private void OnPreCull()
        {
            outlinedRenderers.RemoveWhere(renderer => renderer == null);
            EnsureMaskTexture();
            if (commandsDirty)
            {
                RebuildMaskCommands();
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (compositeMaterial == null || maskTexture == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            compositeMaterial.SetTexture("_MaskTex", maskTexture);
            compositeMaterial.SetColor("_OutlineColor", outlineColor);
            compositeMaterial.SetFloat("_OutlinePixels", outlinePixels);
            Graphics.Blit(source, destination, compositeMaterial);
        }

        private void OnDisable()
        {
            RemoveCommandBuffer();
            if (instance == this) instance = null;
        }

        private void OnDestroy()
        {
            RemoveCommandBuffer();
            ReleaseRuntimeObject(maskMaterial);
            ReleaseRuntimeObject(compositeMaterial);
            ReleaseMaskTexture();
            if (maskCommands != null)
            {
                maskCommands.Release();
                maskCommands = null;
            }

            if (instance == this) instance = null;
        }

        private void CreateMaterials()
        {
            Shader maskShader =
                UnityEngine.Resources.Load<Shader>("Shaders/ScavengeMask") ??
                Shader.Find(MaskShaderName);
            Shader compositeShader =
                UnityEngine.Resources.Load<Shader>("Shaders/ScavengeOutlineComposite") ??
                Shader.Find(CompositeShaderName);
            if (maskShader == null || compositeShader == null)
            {
                Debug.LogError(
                    $"[{nameof(ScavengeScreenOutline)}] Required outline shaders could not be found.");
                return;
            }

            maskMaterial = new Material(maskShader)
            {
                name = "Scavenge Mask (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            compositeMaterial = new Material(compositeShader)
            {
                name = "Scavenge Outline Composite (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void RefreshCollectibles()
        {
            ScavengeCollectible[] collectibles =
                FindObjectsByType<ScavengeCollectible>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ScavengeCollectible collectible in collectibles)
            {
                if (collectible.gameObject.scene != gameObject.scene) continue;
                RegisterRenderers(collectible);
            }
        }

        private void RegisterRenderers(ScavengeCollectible collectible)
        {
            Renderer[] renderers = collectible.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                if (outlinedRenderers.Add(renderer))
                {
                    commandsDirty = true;
                }
            }
        }

        private void EnsureMaskTexture()
        {
            int width = Mathf.Max(1, targetCamera.pixelWidth);
            int height = Mathf.Max(1, targetCamera.pixelHeight);
            if (maskTexture != null && maskWidth == width && maskHeight == height) return;

            ReleaseMaskTexture();
            maskWidth = width;
            maskHeight = height;
            maskTexture = new RenderTexture(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear)
            {
                name = "Scavenge Outline Mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            maskTexture.Create();
            commandsDirty = true;
        }

        private void RebuildMaskCommands()
        {
            if (maskMaterial == null || maskTexture == null || targetCamera == null) return;

            if (maskCommands == null)
            {
                maskCommands = new CommandBuffer { name = "Scavenge Outer Silhouette Mask" };
            }

            if (!commandBufferAttached)
            {
                targetCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, maskCommands);
                commandBufferAttached = true;
            }

            maskCommands.Clear();
            maskCommands.SetRenderTarget(
                new RenderTargetIdentifier(maskTexture),
                BuiltinRenderTextureType.Depth);
            maskCommands.ClearRenderTarget(false, true, Color.clear);

            foreach (Renderer renderer in outlinedRenderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                int subMeshCount = GetSubMeshCount(renderer);
                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    maskCommands.DrawRenderer(renderer, maskMaterial, subMesh, 0);
                }
            }

            commandsDirty = false;
        }

        private static int GetSubMeshCount(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                return Mathf.Max(1, skinned.sharedMesh.subMeshCount);
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? Mathf.Max(1, filter.sharedMesh.subMeshCount)
                : 1;
        }

        private void RemoveCommandBuffer()
        {
            if (targetCamera != null && maskCommands != null)
            {
                targetCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, maskCommands);
            }

            commandBufferAttached = false;
        }

        private void ReleaseMaskTexture()
        {
            if (maskTexture == null) return;
            maskTexture.Release();
            ReleaseRuntimeObject(maskTexture);
            maskTexture = null;
        }

        private static void ReleaseRuntimeObject(Object runtimeObject)
        {
            if (runtimeObject == null) return;
            if (Application.isPlaying) Destroy(runtimeObject);
            else DestroyImmediate(runtimeObject);
        }
    }
}
