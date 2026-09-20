using UnityEngine;
using UnityEngine.SceneManagement;
using YesterdayMap.UI;

namespace YesterdayMap.Scavenge
{
    internal static class ScavengeSceneRuntimeFixes
    {
        private const string ScavengeSceneName = "Scavenge";
        private const string DiningCabinetPath =
            "EditableFurniture/RusticDarkWoodKitchenCabinet";
        private const string BunkerVisualName = "BunkerEntranceVisual";
        private const string BunkerMarkerName = "BunkerEntranceMarker";
        private const float BunkerMarkerGap = 0.18f;

        private static readonly Vector3 VisibleBunkerHatchPosition =
            new Vector3(14.1f, 0.15f, -10.85f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Apply()
        {
            if (SceneManager.GetActiveScene().name != ScavengeSceneName)
            {
                return;
            }

            AlignBunkerInteractionWithVisibleHatch();
            AlignDiningCabinetColliderWithVisual();
        }

        private static void AlignBunkerInteractionWithVisibleHatch()
        {
            BunkerEntrance entrance = Object.FindFirstObjectByType<BunkerEntrance>();
            if (entrance == null)
            {
                return;
            }

            GameObject visualObject = GameObject.Find(BunkerVisualName);
            Transform visual = visualObject != null ? visualObject.transform : null;

            // Follow the scene's editable hatch placement instead of restoring an old fixed coordinate.
            entrance.transform.position = visual != null
                ? visual.position
                : VisibleBunkerHatchPosition;
            if (visual != null)
            {
                entrance.transform.rotation = Quaternion.Euler(0f, visual.eulerAngles.y, 0f);
            }

            // The trigger itself handles range so the player can approach from any unblocked side.
            entrance.SetInteractionPoint(null, 0.18f);

            BoxCollider entranceCollider = entrance.GetComponent<BoxCollider>();
            if (entranceCollider != null)
            {
                entranceCollider.isTrigger = true;
            }

            MeshRenderer markerRenderer = entrance.GetComponent<MeshRenderer>();
            if (markerRenderer != null)
            {
                markerRenderer.enabled = false;
            }

            AlignBunkerMarker(visual, entrance.transform);
        }

        private static void AlignBunkerMarker(Transform visual, Transform fallbackTarget)
        {
            GameObject markerObject = GameObject.Find(BunkerMarkerName);
            if (markerObject == null ||
                !markerObject.TryGetComponent(out BobbingWorldMarker marker))
            {
                return;
            }

            Transform target = visual != null ? visual : fallbackTarget;
            float height = CalculateMarkerHeight(target, markerObject);
            Vector3 offset = Vector3.up * height;
            marker.Configure(target, offset, 0.24f, 2f);
            markerObject.transform.position = target.position + offset;
        }

        private static float CalculateMarkerHeight(Transform target, GameObject markerObject)
        {
            float highestPoint = target.position.y + 2f;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                highestPoint = Mathf.Max(highestPoint, renderer.bounds.max.y);
            }

            Renderer arrowRenderer = markerObject.GetComponentInChildren<Renderer>(true);
            float arrowHalfHeight = arrowRenderer != null
                ? arrowRenderer.bounds.extents.y
                : 0.6f;

            return highestPoint - target.position.y + arrowHalfHeight + BunkerMarkerGap;
        }

        private static void AlignDiningCabinetColliderWithVisual()
        {
            GameObject cabinet = GameObject.Find(DiningCabinetPath);
            if (cabinet == null)
            {
                return;
            }

            BoxCollider cabinetCollider = cabinet.GetComponent<BoxCollider>();
            Renderer visualRenderer = cabinet.GetComponentInChildren<Renderer>();
            if (cabinetCollider == null || visualRenderer == null)
            {
                return;
            }

            Bounds worldBounds = visualRenderer.bounds;
            Vector3 minimum = worldBounds.min;
            Vector3 maximum = worldBounds.max;

            Bounds localBounds = new Bounds(
                cabinet.transform.InverseTransformPoint(minimum),
                Vector3.zero);

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? minimum.x : maximum.x,
                            y == 0 ? minimum.y : maximum.y,
                            z == 0 ? minimum.z : maximum.z);

                        localBounds.Encapsulate(
                            cabinet.transform.InverseTransformPoint(corner));
                    }
                }
            }

            cabinetCollider.center = localBounds.center;
            cabinetCollider.size = localBounds.size;
        }
    }
}
