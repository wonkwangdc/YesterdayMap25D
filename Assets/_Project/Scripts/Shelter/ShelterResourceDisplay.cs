using System;
using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.Resources;

namespace YesterdayMap.Shelter
{
    // 벙커 창고에 현재 식량, 식수, 구급상자, 연료 수량을 실제 아이템 모델로 표시한다.
    public sealed class ShelterResourceDisplay : MonoBehaviour
    {
        private const string DisplayPrefabPath = "Shelter/ShelterResourceDisplay";
        private const string ShelfBaseName = "BunkerProp_IronVault";
        private const string WaterShelfName = "BunkerProp_IronVault (1)";
        private const string MedicineShelfName = "BunkerProp_IronVault (2)";
        private const string FuelDisplayAnchorName = "Collected_FuelCanPreview";
        private const int ResourceUnitsPerVisual = 1;
        private const int Columns = 4;
        private const int MaximumVisibleItems = 16;

        [SerializeField] private GameObject foodPrefab;
        [SerializeField] private GameObject waterPrefab;
        [SerializeField] private GameObject medicinePrefab;
        [SerializeField] private GameObject fuelPrefab;
        [SerializeField, Range(0.2f, 1f)] private float itemScale = 0.62f;

        private readonly List<GameObject> foodVisuals = new();
        private readonly List<GameObject> waterVisuals = new();
        private readonly List<GameObject> medicineVisuals = new();
        private readonly List<GameObject> fuelVisuals = new();
        private ResourceManager resources;
        private Bounds foodShelfBounds;
        private Bounds waterShelfBounds;
        private Bounds medicineShelfBounds;
        private Transform fuelDisplayAnchor;
        private int trackedFoodUnits;
        private int trackedWaterUnits;
        private int trackedMedicineUnits;
        private int trackedFuelUnits;
        private int lastFoodUnits;
        private int lastWaterUnits;
        private int lastMedicineUnits;
        private int lastFuelUnits;
        private bool hasShelfLayout;
        private bool hasMedicineShelf;
        private bool subscribed;
        private bool missingFuelPrefabReported;

        public int VisibleFoodCount => CountActive(foodVisuals);
        public int VisibleWaterCount => CountActive(waterVisuals);
        public int VisibleMedicineCount => CountActive(medicineVisuals);
        public int VisibleFuelCount => CountActive(fuelVisuals);

        public static bool TryLoadConsumablePrefabs(
            out GameObject food,
            out GameObject water)
        {
            GameObject template =
                UnityEngine.Resources.Load<GameObject>(DisplayPrefabPath);
            ShelterResourceDisplay display = template != null
                ? template.GetComponent<ShelterResourceDisplay>()
                : null;
            food = display != null ? display.foodPrefab : null;
            water = display != null ? display.waterPrefab : null;
            return food != null && water != null;
        }

        public static ShelterResourceDisplay Ensure(
            ResourceManager manager,
            int hiddenStartingFoodUnits,
            int hiddenStartingWaterUnits)
        {
            ShelterResourceDisplay display = FindFirstObjectByType<ShelterResourceDisplay>();
            if (display == null)
            {
                GameObject template =
                    UnityEngine.Resources.Load<GameObject>(DisplayPrefabPath);
                if (template == null)
                {
                    Debug.LogWarning(
                        $"Shelter resource display prefab was not found at Resources/{DisplayPrefabPath}.");
                    return null;
                }

                GameObject instance = Instantiate(template);
                instance.name = "ShelterResourceDisplay";
                display = instance.GetComponent<ShelterResourceDisplay>();
            }

            display.Configure(
                manager,
                hiddenStartingFoodUnits,
                hiddenStartingWaterUnits);
            return display;
        }

        public void Configure(
            ResourceManager manager,
            int hiddenStartingFoodUnits,
            int hiddenStartingWaterUnits)
        {
            Unsubscribe();
            resources = manager;
            lastFoodUnits = resources != null
                ? resources.GetAmount(ResourceType.Food)
                : 0;
            lastWaterUnits = resources != null
                ? resources.GetAmount(ResourceType.Water)
                : 0;
            lastMedicineUnits = resources != null
                ? resources.GetAmount(ResourceType.Medicine)
                : 0;
            lastFuelUnits = resources != null
                ? resources.GetAmount(ResourceType.Fuel)
                : 0;
            trackedFoodUnits = Mathf.Max(
                0,
                lastFoodUnits - Mathf.Max(0, hiddenStartingFoodUnits));
            trackedWaterUnits = Mathf.Max(
                0,
                lastWaterUnits - Mathf.Max(0, hiddenStartingWaterUnits));
            trackedMedicineUnits = Mathf.Max(0, lastMedicineUnits);
            trackedFuelUnits = Mathf.Max(0, lastFuelUnits);
            ResolveShelfLayout();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (resources == null || subscribed) return;
            resources.ResourcesChanged += HandleResourcesChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (resources != null && subscribed)
            {
                resources.ResourcesChanged -= HandleResourcesChanged;
            }

            subscribed = false;
        }

        public void Refresh()
        {
            if (resources == null || !hasShelfLayout) return;

            int foodCount = VisibleItemCount(trackedFoodUnits);
            int waterCount = VisibleItemCount(trackedWaterUnits);
            int medicineCount = VisibleItemCount(trackedMedicineUnits, 1);
            int fuelCount = Mathf.Max(0, trackedFuelUnits);

            SyncVisuals(
                foodVisuals,
                foodCount,
                foodPrefab,
                foodShelfBounds,
                "StoredFood");
            SyncVisuals(
                waterVisuals,
                waterCount,
                waterPrefab,
                waterShelfBounds,
                "StoredWater");

            if (hasMedicineShelf)
            {
                SyncVisuals(
                    medicineVisuals,
                    medicineCount,
                    medicinePrefab,
                    medicineShelfBounds,
                    "StoredMedicine");
            }

            SyncFuelVisuals(
                fuelCount,
                hasMedicineShelf ? medicineShelfBounds : waterShelfBounds);
        }

        private void HandleResourcesChanged()
        {
            int currentFoodUnits = resources.GetAmount(ResourceType.Food);
            int currentWaterUnits = resources.GetAmount(ResourceType.Water);
            int currentMedicineUnits = resources.GetAmount(ResourceType.Medicine);
            int currentFuelUnits = resources.GetAmount(ResourceType.Fuel);

            trackedFoodUnits = Mathf.Clamp(
                trackedFoodUnits + currentFoodUnits - lastFoodUnits,
                0,
                currentFoodUnits);
            trackedWaterUnits = Mathf.Clamp(
                trackedWaterUnits + currentWaterUnits - lastWaterUnits,
                0,
                currentWaterUnits);
            trackedMedicineUnits = Mathf.Clamp(
                trackedMedicineUnits + currentMedicineUnits - lastMedicineUnits,
                0,
                currentMedicineUnits);
            trackedFuelUnits = Mathf.Clamp(
                trackedFuelUnits + currentFuelUnits - lastFuelUnits,
                0,
                currentFuelUnits);
            lastFoodUnits = currentFoodUnits;
            lastWaterUnits = currentWaterUnits;
            lastMedicineUnits = currentMedicineUnits;
            lastFuelUnits = currentFuelUnits;
            Refresh();
        }

        private void ResolveShelfLayout()
        {
            Transform foodShelf = null;
            Transform waterShelf = null;
            Transform medicineShelf = null;
            var shelfCandidates = new List<Transform>();
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Transform candidate in sceneTransforms)
            {
                if (candidate.gameObject.scene != gameObject.scene) continue;
                if (candidate.name == FuelDisplayAnchorName)
                {
                    fuelDisplayAnchor = candidate;
                }

                if (!candidate.name.StartsWith(
                        ShelfBaseName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                shelfCandidates.Add(candidate);
                if (candidate.name == ShelfBaseName)
                {
                    foodShelf = candidate;
                }
                else if (candidate.name == WaterShelfName)
                {
                    waterShelf = candidate;
                }
                else if (candidate.name == MedicineShelfName)
                {
                    medicineShelf = candidate;
                }
            }

            if (foodShelf == null || waterShelf == null)
            {
                hasShelfLayout = false;
                Debug.LogWarning("The two bunker resource shelves could not be found.");
                return;
            }

            medicineShelf ??= FindAdditionalShelf(
                shelfCandidates,
                foodShelf,
                waterShelf);

            hasShelfLayout =
                TryGetRendererBounds(foodShelf, out foodShelfBounds) &&
                TryGetRendererBounds(waterShelf, out waterShelfBounds);
            hasMedicineShelf = medicineShelf != null &&
                TryGetRendererBounds(medicineShelf, out medicineShelfBounds);

            if (hasShelfLayout)
            {
                StorageObject.InstallShelfInteractions(
                    resources,
                    foodShelf,
                    waterShelf,
                    hasMedicineShelf ? medicineShelf : null);
            }
        }

        private static Transform FindAdditionalShelf(
            List<Transform> shelfCandidates,
            Transform foodShelf,
            Transform waterShelf)
        {
            Transform bestCandidate = null;
            float bestDistance = float.PositiveInfinity;
            Vector3 expectedPosition = NextShelfPosition(
                foodShelf,
                waterShelf);

            foreach (Transform candidate in shelfCandidates)
            {
                if (candidate == null ||
                    candidate == foodShelf ||
                    candidate == waterShelf ||
                    candidate.parent != foodShelf.parent)
                {
                    continue;
                }

                float distance =
                    (candidate.localPosition - expectedPosition).sqrMagnitude;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                bestCandidate = candidate;
            }

            if (bestCandidate != null)
            {
                bestCandidate.name = MedicineShelfName;
            }

            return bestCandidate;
        }

        private static Vector3 NextShelfPosition(
            Transform foodShelf,
            Transform waterShelf)
        {
            Vector3 spacing =
                waterShelf.localPosition - foodShelf.localPosition;
            if (spacing.sqrMagnitude < 0.01f)
            {
                spacing = Vector3.forward * 2.4f;
            }

            return waterShelf.localPosition + spacing;
        }

        private void SyncVisuals(
            List<GameObject> visuals,
            int targetCount,
            GameObject prefab,
            Bounds shelfBounds,
            string itemName)
        {
            if (prefab == null) return;

            while (visuals.Count < targetCount)
            {
                GameObject visual = Instantiate(prefab, transform);
                visual.name = $"{itemName}_{visuals.Count + 1:00}";
                visuals.Add(visual);
            }

            for (int index = 0; index < visuals.Count; index++)
            {
                GameObject visual = visuals[index];
                bool shouldShow = index < targetCount;
                visual.SetActive(shouldShow);
                if (!shouldShow) continue;

                visual.transform.position = SlotPosition(shelfBounds, index);
                visual.transform.rotation =
                    Quaternion.Euler(0f, index % 2 == 0 ? -4f : 4f, 0f);
                visual.transform.localScale = Vector3.one * itemScale;
            }
        }

        private void SyncFuelVisuals(int targetCount, Bounds referenceBounds)
        {
            if (fuelPrefab == null)
            {
                if (!missingFuelPrefabReported)
                {
                    Debug.LogWarning(
                        "Stored fuel can prefab is not assigned to ShelterResourceDisplay.");
                    missingFuelPrefabReported = true;
                }

                return;
            }

            while (fuelVisuals.Count < targetCount)
            {
                GameObject visual = Instantiate(fuelPrefab, transform);
                visual.name = $"StoredFuel_{fuelVisuals.Count + 1:00}";
                fuelVisuals.Add(visual);
            }

            for (int index = 0; index < fuelVisuals.Count; index++)
            {
                GameObject visual = fuelVisuals[index];
                FuelCanObject pickup =
                    visual.GetComponent<FuelCanObject>();
                pickup?.Configure(resources, this);

                bool shouldShow = index < targetCount;
                visual.SetActive(shouldShow);
                if (!shouldShow) continue;

                PlaceFuelVisual(visual.transform, referenceBounds, index);
                visual.transform.localScale = Vector3.one;
            }
        }

        private void PlaceFuelVisual(
            Transform visual,
            Bounds referenceBounds,
            int index)
        {
            float angleOffset = index % 2 == 0 ? -5f : 5f;
            if (fuelDisplayAnchor == null)
            {
                visual.position = FuelSlotPosition(referenceBounds, index);
                visual.rotation = Quaternion.Euler(0f, angleOffset, 0f);
                return;
            }

            int column = index % Columns;
            int row = index / Columns;
            visual.position = fuelDisplayAnchor.TransformPoint(
                new Vector3(column * 0.48f, 0f, row * 0.32f));
            visual.rotation = fuelDisplayAnchor.rotation *
                Quaternion.Euler(0f, angleOffset, 0f);
        }

        public bool DetachFuelVisualForPickup(GameObject visual)
        {
            if (visual == null)
            {
                return false;
            }

            if (!fuelVisuals.Remove(visual))
            {
                return false;
            }

            // 수량이 줄 때 마지막 항목이 비활성화되므로,
            // 실제로 집은 빨간 연료통을 목록 끝으로 보내 정확히 숨깁니다.
            visual.SetActive(false);
            return true;
        }

        public void RestoreDetachedFuelVisual(GameObject visual)
        {
            if (visual == null || fuelVisuals.Contains(visual))
            {
                return;
            }

            fuelVisuals.Add(visual);
            visual.SetActive(true);
            Refresh();
        }

        public void FinalizeFuelVisualRemoval(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            // Keep the display list and the picked scene object in sync.
            // Deactivation is immediate; destruction is finalized at frame end.
            fuelVisuals.Remove(visual);
            visual.SetActive(false);
            Destroy(visual);
        }

        private static Vector3 FuelSlotPosition(Bounds referenceBounds, int index)
        {
            int column = index % Columns;
            int row = index / Columns;
            return new Vector3(
                referenceBounds.max.x + 0.58f + row * 0.52f,
                referenceBounds.min.y,
                referenceBounds.max.z + 0.48f + column * 0.62f);
        }

        private static Vector3 SlotPosition(Bounds shelfBounds, int index)
        {
            int column = index % Columns;
            int row = index / Columns;

            float zMargin = 0.36f;
            float z = Mathf.Lerp(
                shelfBounds.min.z + zMargin,
                shelfBounds.max.z - zMargin,
                column / (float)(Columns - 1));
            float y = shelfBounds.min.y + 0.42f + row * 0.72f;
            float x = shelfBounds.max.x - 0.28f;
            return new Vector3(x, y, z);
        }

        private static int VisibleItemCount(
            int resourceUnits,
            int resourceUnitsPerVisual = ResourceUnitsPerVisual)
        {
            return Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(0, resourceUnits) /
                    (float)Mathf.Max(1, resourceUnitsPerVisual)),
                0,
                MaximumVisibleItems);
        }

        private static int CountActive(List<GameObject> visuals)
        {
            int count = 0;
            foreach (GameObject visual in visuals)
            {
                if (visual != null && visual.activeSelf) count++;
            }

            return count;
        }

        private static bool TryGetRendererBounds(
            Transform root,
            out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }
    }
}
