using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.Audio;
using YesterdayMap.Interaction;
using YesterdayMap.Resources;

namespace YesterdayMap.Scavenge
{
    public sealed class ScavengeCollectible : InteractableObject
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private ScavengeManager manager;
        [SerializeField] private ScavengeItemKind itemKind;
        private readonly List<Transform> visualRoots = new();

        public override string InteractionPrompt => $"[E] {ScavengeManager.KoreanName(resourceType, itemKind)} 줍기";
        public ResourceType ResourceType => resourceType;
        public ScavengeItemKind ItemKind => itemKind;
        public int VisualPickupCount
        {
            get
            {
                if (visualRoots.Count == 0)
                {
                    EnsureVisualInteractionColliders();
                }

                int count = 0;
                foreach (Transform visualRoot in visualRoots)
                {
                    if (visualRoot != null && visualRoot.gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public override bool RequiresCenterAim => true;

        private void Awake()
        {
            EnsureVisualInteractionColliders();
        }

        private void OnEnable()
        {
            ScavengeScreenOutline.Register(this);
        }

        private void OnDisable()
        {
            ScavengeScreenOutline.Unregister(this);
        }

        public void Configure(ResourceType type, int value, ScavengeManager scavengeManager)
        {
            resourceType = type;
            amount = value;
            manager = scavengeManager;
        }

        public override void Interact()
        {
            Transform visualRoot = FindFirstActiveVisualRoot();
            if (visualRoot != null)
            {
                TryCollectVisual(visualRoot);
            }
        }

        public bool TryCollectVisual(Transform visualRoot)
        {
            if (visualRoot == null || !visualRoot.gameObject.activeInHierarchy)
            {
                return false;
            }

            int collectedAmount = itemKind == ScavengeItemKind.Resource
                ? 1
                : amount;
            if (manager == null ||
                !manager.TryCollect(resourceType, collectedAmount, itemKind))
            {
                return false;
            }

            GameSfxPlayer.Play(GameSfxCue.Pickup);
            ScavengeScreenOutline.UnregisterRenderers(visualRoot);

            if (visualRoot == transform)
            {
                gameObject.SetActive(false);
                return true;
            }

            visualRoot.gameObject.SetActive(false);
            if (!HasActiveVisualRoots())
            {
                gameObject.SetActive(false);
            }

            return true;
        }
        private void EnsureVisualInteractionColliders()
        {
            visualRoots.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Collider[] existingColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider existingCollider in existingColliders)
            {
                if (existingCollider != null)
                {
                    existingCollider.enabled = false;
                }
            }

            Dictionary<Transform, List<Renderer>> rendererGroups = new();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                Transform visualRoot = FindDirectVisualRoot(renderer.transform);
                if (!rendererGroups.TryGetValue(visualRoot, out List<Renderer> group))
                {
                    group = new List<Renderer>();
                    rendererGroups.Add(visualRoot, group);
                    visualRoots.Add(visualRoot);
                }

                group.Add(renderer);
            }

            foreach (KeyValuePair<Transform, List<Renderer>> entry in rendererGroups)
            {
                Transform visualRoot = entry.Key;
                BoxCollider interactionCollider = visualRoot.GetComponent<BoxCollider>();
                if (interactionCollider == null)
                {
                    interactionCollider = visualRoot.gameObject.AddComponent<BoxCollider>();
                }

                interactionCollider.enabled = true;
                interactionCollider.isTrigger = true;
                FitColliderToRenderers(interactionCollider, entry.Value);

                if (visualRoot == transform) continue;

                ScavengeCollectibleVisualProxy proxy =
                    visualRoot.GetComponent<ScavengeCollectibleVisualProxy>();
                if (proxy == null)
                {
                    proxy = visualRoot.gameObject.AddComponent<ScavengeCollectibleVisualProxy>();
                }

                proxy.Configure(this, visualRoot);
            }
        }
        private static void FitColliderToRenderers(
            BoxCollider collider,
            IReadOnlyList<Renderer> renderers)
        {
            bool hasBounds = false;
            Bounds worldBounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds) return;

            Vector3 minimum = worldBounds.min;
            Vector3 maximum = worldBounds.max;
            Transform colliderTransform = collider.transform;
            Bounds localBounds = new(
                colliderTransform.InverseTransformPoint(worldBounds.center),
                Vector3.zero);

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? minimum.x : maximum.x,
                            y == 0 ? minimum.y : maximum.y,
                            z == 0 ? minimum.z : maximum.z);
                        localBounds.Encapsulate(
                            colliderTransform.InverseTransformPoint(corner));
                    }
                }
            }

            collider.center = localBounds.center;
            Vector3 scale = colliderTransform.lossyScale;
            Vector3 minimumLocalSize = new(
                0.02f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                0.02f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                0.02f / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            collider.size = new Vector3(
                Mathf.Max(minimumLocalSize.x, localBounds.size.x * 1.08f),
                Mathf.Max(minimumLocalSize.y, localBounds.size.y * 1.08f),
                Mathf.Max(minimumLocalSize.z, localBounds.size.z * 1.08f));
        }

        private Transform FindDirectVisualRoot(Transform rendererTransform)
        {
            Transform current = rendererTransform;
            while (current.parent != null && current.parent != transform)
            {
                current = current.parent;
            }

            return current.parent == transform ? current : transform;
        }
        private bool HasActiveVisualRoots()
        {
            foreach (Transform visualRoot in visualRoots)
            {
                if (visualRoot != null &&
                    visualRoot != transform &&
                    visualRoot.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }
        private Transform FindFirstActiveVisualRoot()
        {
            if (visualRoots.Count == 0)
            {
                EnsureVisualInteractionColliders();
            }

            foreach (Transform visualRoot in visualRoots)
            {
                if (visualRoot != null && visualRoot.gameObject.activeInHierarchy)
                {
                    return visualRoot;
                }
            }

            return null;
        }
    }
}
