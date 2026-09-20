using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.Shelter;

namespace YesterdayMap.Events
{
    // Pulses the Shelter door outline until today's event dialogue has been viewed.
    public sealed class ShelterEventMarker : MonoBehaviour
    {
        private const string OutlineShaderName =
            "YesterdayMap/WorldEventOutline";
        private const float PulseSpeed = 4.2f;
        private const float MinimumAlpha = 0.48f;
        private const float MaximumAlpha = 1f;
        private const float MinimumWidth = 3.5f;
        private const float MaximumWidth = 5f;
        private static readonly int OutlineColorId =
            Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineAlphaId =
            Shader.PropertyToID("_OutlineAlpha");
        private static readonly int OutlineWidthId =
            Shader.PropertyToID("_OutlineWidth");

        [SerializeField] private ShelterEventDialogueController eventDialogue;
        [SerializeField] private Transform doorVisualRoot;

        // Kept only so older scenes disable their former exclamation visual safely.
        [SerializeField] private GameObject visualRoot;

        private readonly List<Renderer> outlinedRenderers = new();
        private Material outlineMaterial;
        private bool? lastVisibleState;
        private bool outlineShaderMissing;

        public void Configure(
            ShelterEventDialogueController dialogueController,
            GameObject doorVisual)
        {
            eventDialogue = dialogueController;
            doorVisualRoot = doorVisual != null
                ? doorVisual.transform
                : null;
            RefreshVisibility();
        }

        private void OnEnable()
        {
            ResolveReferences();
            DisableLegacyMarker();
            EnsureOutlineInstalled();
            RefreshVisibility();
        }

        private void Update()
        {
            RefreshVisibility();
            UpdateOutlinePulse();
        }

        private void OnDisable()
        {
            SetOutline(0f, MinimumWidth);
        }

        private void OnDestroy()
        {
            RemoveOutlineMaterial();
        }

        private void ResolveReferences()
        {
            if (eventDialogue == null)
            {
                eventDialogue = FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            }

            if (doorVisualRoot == null)
            {
                DoorObject door = FindFirstObjectByType<DoorObject>(
                    FindObjectsInactive.Include);
                if (door != null)
                {
                    Transform visual =
                        door.transform.Find("MeshyBunkerDoorVisual");
                    doorVisualRoot = visual != null
                        ? visual
                        : door.transform;
                }
            }
        }

        private void RefreshVisibility()
        {
            ResolveReferences();

            bool shouldBeVisible = eventDialogue != null && eventDialogue.ShouldShowEventMarker;
            if (lastVisibleState == shouldBeVisible)
            {
                return;
            }

            lastVisibleState = shouldBeVisible;
            if (!shouldBeVisible)
            {
                SetOutline(0f, MinimumWidth);
            }
        }

        private void DisableLegacyMarker()
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }

        private void EnsureOutlineInstalled()
        {
            if (!Application.isPlaying ||
                outlineMaterial != null ||
                outlineShaderMissing)
            {
                return;
            }

            ResolveReferences();
            if (doorVisualRoot == null)
            {
                return;
            }

            Shader shader = Shader.Find(OutlineShaderName);
            if (shader == null)
            {
                outlineShaderMissing = true;
                Debug.LogWarning(
                    $"Shelter door outline shader was not found: {OutlineShaderName}",
                    this);
                return;
            }

            outlineMaterial = new Material(shader)
            {
                name = "Shelter Door Event Outline (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            SetOutline(0f, MinimumWidth);

            Renderer[] renderers =
                doorVisualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                Material[] currentMaterials = targetRenderer.sharedMaterials;
                Material[] highlightedMaterials =
                    new Material[currentMaterials.Length + 1];
                currentMaterials.CopyTo(highlightedMaterials, 0);
                highlightedMaterials[^1] = outlineMaterial;
                targetRenderer.sharedMaterials = highlightedMaterials;
                outlinedRenderers.Add(targetRenderer);
            }
        }

        private void UpdateOutlinePulse()
        {
            if (lastVisibleState != true)
            {
                return;
            }

            EnsureOutlineInstalled();
            if (outlineMaterial == null)
            {
                return;
            }

            float pulse =
                (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) * 0.5f;
            SetOutline(
                Mathf.Lerp(MinimumAlpha, MaximumAlpha, pulse),
                Mathf.Lerp(MinimumWidth, MaximumWidth, pulse));
        }

        private void SetOutline(float alpha, float width)
        {
            if (outlineMaterial == null)
            {
                return;
            }

            outlineMaterial.SetColor(OutlineColorId, Color.white);
            outlineMaterial.SetFloat(OutlineAlphaId, alpha);
            outlineMaterial.SetFloat(OutlineWidthId, width);
        }

        private void RemoveOutlineMaterial()
        {
            if (outlineMaterial == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in outlinedRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                Material[] currentMaterials = targetRenderer.sharedMaterials;
                int retainedCount = 0;
                foreach (Material material in currentMaterials)
                {
                    if (material != outlineMaterial)
                    {
                        retainedCount++;
                    }
                }

                Material[] restoredMaterials = new Material[retainedCount];
                int restoredIndex = 0;
                foreach (Material material in currentMaterials)
                {
                    if (material != outlineMaterial)
                    {
                        restoredMaterials[restoredIndex++] = material;
                    }
                }

                targetRenderer.sharedMaterials = restoredMaterials;
            }

            outlinedRenderers.Clear();
            Destroy(outlineMaterial);
            outlineMaterial = null;
        }
    }
}
