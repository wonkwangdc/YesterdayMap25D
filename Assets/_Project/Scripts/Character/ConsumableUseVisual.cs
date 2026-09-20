using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using YesterdayMap.Audio;
using YesterdayMap.Shelter;

namespace YesterdayMap.Character
{
    public enum ConsumableVisualKind
    {
        Food,
        Water
    }

    [DisallowMultipleComponent]
    public sealed class ConsumableUseVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GameObject foodPrefab;
        [SerializeField] private GameObject waterPrefab;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float raiseDuration = 0.32f;
        [SerializeField, Min(0.1f)] private float foodUseDuration = 0.62f;
        // Water_Drink.mp3 is trimmed to 3s: 0.32s raise + 2.44s sip + 0.24s lower.
        [SerializeField, Min(0.1f)] private float waterUseDuration = 2.44f;
        [SerializeField, Min(0.05f)] private float lowerDuration = 0.24f;
        [SerializeField, Min(0f)] private float combinedPause = 0.12f;

        [Header("First Person Pose")]
        [SerializeField] private Vector3 startLocalPosition =
            new(0.34f, -0.32f, 0.82f);
        [SerializeField] private Vector3 foodUseLocalPosition =
            new(0.08f, -0.12f, 0.62f);
        [SerializeField] private Vector3 waterUseLocalPosition =
            new(0.07f, -0.10f, 0.60f);
        [SerializeField] private Vector3 foodEuler =
            new(5f, -25f, -8f);
        [SerializeField] private Vector3 waterEuler =
            new(0f, -6f, 0f);
        [SerializeField] private Vector3 waterTiltEuler =
            new(-68f, -6f, 0f);
        [SerializeField, Min(0.05f)] private float foodScale = 0.30f;
        [SerializeField, Min(0.05f)] private float waterScale = 0.27f;
        [SerializeField, Min(0f)] private float raiseArcHeight = 0.07f;
        [SerializeField] private AnimationCurve movementCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine useRoutine;
        private Transform visualRoot;
        private GameObject activeItem;
        private bool interactionWasEnabled;
        private bool inputLocked;

        public bool IsPlaying => useRoutine != null;

        public static void Play(bool useFood, bool useWater)
        {
            if (!useFood && !useWater) return;

            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player == null) return;

            ConsumableUseVisual visual = player.GetComponent<ConsumableUseVisual>();
            if (visual == null)
            {
                visual = player.gameObject.AddComponent<ConsumableUseVisual>();
            }

            visual.PlayInternal(useFood, useWater);
        }

        public static void Play(ConsumableVisualKind kind)
        {
            Play(kind == ConsumableVisualKind.Food, kind == ConsumableVisualKind.Water);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private void OnDestroy()
        {
            if (visualRoot != null)
            {
                Destroy(visualRoot.gameObject);
            }
        }

        private void PlayInternal(bool useFood, bool useWater)
        {
            ResolveReferences();
            if (playerMovement == null || targetCamera == null || !ResolvePrefabs())
            {
                return;
            }

            StopAndRestore();
            LockPlayer();
            useRoutine = StartCoroutine(PlaySequence(useFood, useWater));
        }

        private IEnumerator PlaySequence(bool useFood, bool useWater)
        {
            try
            {
                EnsureVisualRoot();

                if (useFood)
                {
                    yield return AnimateItem(ConsumableVisualKind.Food);
                }

                if (useFood && useWater && combinedPause > 0f)
                {
                    yield return new WaitForSecondsRealtime(combinedPause);
                }

                if (useWater)
                {
                    if (useFood)
                    {
                        GameSfxPlayer.Play(GameSfxCue.DrinkWater);
                    }

                    yield return AnimateItem(ConsumableVisualKind.Water);
                }
            }
            finally
            {
                useRoutine = null;
                HideItem();
                RestorePlayer();
            }
        }

        private IEnumerator AnimateItem(ConsumableVisualKind kind)
        {
            GameObject prefab = kind == ConsumableVisualKind.Food
                ? foodPrefab
                : waterPrefab;
            if (prefab == null) yield break;

            CreateItem(prefab, kind);
            Vector3 usePosition = kind == ConsumableVisualKind.Food
                ? foodUseLocalPosition
                : waterUseLocalPosition;
            Vector3 baseEuler = kind == ConsumableVisualKind.Food
                ? foodEuler
                : waterEuler;
            float itemScale = kind == ConsumableVisualKind.Food
                ? foodScale
                : waterScale;

            float elapsed = 0f;
            while (elapsed < raiseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / raiseDuration);
                float eased = movementCurve.Evaluate(normalized);
                Vector3 position = Vector3.Lerp(startLocalPosition, usePosition, eased);
                position.y += Mathf.Sin(normalized * Mathf.PI) * raiseArcHeight;
                SetItemPose(
                    position,
                    Vector3.Lerp(Vector3.zero, baseEuler, eased),
                    itemScale * Mathf.SmoothStep(0f, 1f, normalized));
                yield return null;
            }

            float useDuration = kind == ConsumableVisualKind.Food
                ? foodUseDuration
                : waterUseDuration;
            elapsed = 0f;
            while (elapsed < useDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / useDuration);
                if (kind == ConsumableVisualKind.Water)
                {
                    float sip = Mathf.Sin(normalized * Mathf.PI);
                    Vector3 position = waterUseLocalPosition;
                    position.y += 0.025f * sip;
                    position.z -= 0.035f * sip;
                    SetItemPose(
                        position,
                        Vector3.Lerp(waterEuler, waterTiltEuler, sip),
                        waterScale);
                }
                else
                {
                    float bites = 0.5f + 0.5f * Mathf.Cos(normalized * Mathf.PI * 4f);
                    Vector3 position = foodUseLocalPosition +
                        new Vector3(0.045f * bites, -0.025f * bites, 0f);
                    SetItemPose(
                        position,
                        foodEuler + new Vector3(0f, 0f, 4f * bites),
                        foodScale);
                }

                yield return null;
            }

            Vector3 lowerStartPosition = visualRoot.localPosition;
            Vector3 lowerStartEuler = visualRoot.localEulerAngles;
            elapsed = 0f;
            while (elapsed < lowerDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / lowerDuration);
                float eased = movementCurve.Evaluate(normalized);
                SetItemPose(
                    Vector3.Lerp(lowerStartPosition, startLocalPosition, eased),
                    Vector3.Lerp(lowerStartEuler, Vector3.zero, eased),
                    itemScale * (1f - Mathf.SmoothStep(0f, 1f, normalized)));
                yield return null;
            }

            HideItem();
        }

        private void LockPlayer()
        {
            interactionWasEnabled = playerInteraction != null && playerInteraction.enabled;
            if (playerInteraction != null)
            {
                playerInteraction.enabled = false;
            }

            playerMovement.SetCinematicInputLocked(true);
            inputLocked = true;
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsMoving", false);
                playerAnimator.SetFloat("MoveSpeed", 0f);
            }
        }

        private void RestorePlayer()
        {
            if (!inputLocked)
            {
                return;
            }

            if (playerMovement != null)
            {
                playerMovement.SetCinematicInputLocked(false);
            }

            if (playerInteraction != null)
            {
                playerInteraction.enabled = interactionWasEnabled;
            }

            inputLocked = false;
        }

        private void StopAndRestore()
        {
            if (useRoutine != null)
            {
                StopCoroutine(useRoutine);
                useRoutine = null;
            }

            HideItem();
            RestorePlayer();
        }

        private void ResolveReferences()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (playerInteraction == null)
                playerInteraction = GetComponent<PlayerInteraction>();
            if (playerAnimator == null)
                playerAnimator = GetComponentInChildren<Animator>(true);
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private bool ResolvePrefabs()
        {
            if (foodPrefab != null && waterPrefab != null) return true;

            if (!ShelterResourceDisplay.TryLoadConsumablePrefabs(
                    out GameObject loadedFood,
                    out GameObject loadedWater))
            {
                Debug.LogWarning("Food and water use prefabs could not be loaded.", this);
                return false;
            }

            foodPrefab ??= loadedFood;
            waterPrefab ??= loadedWater;
            return foodPrefab != null && waterPrefab != null;
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                if (visualRoot.parent != targetCamera.transform)
                {
                    visualRoot.SetParent(targetCamera.transform, false);
                }
                return;
            }

            visualRoot = new GameObject("ConsumableUseVisual").transform;
            visualRoot.SetParent(targetCamera.transform, false);
            visualRoot.gameObject.SetActive(false);
        }

        private void CreateItem(GameObject prefab, ConsumableVisualKind kind)
        {
            HideItem();
            visualRoot.gameObject.SetActive(true);
            activeItem = Instantiate(prefab, visualRoot);
            activeItem.name = kind == ConsumableVisualKind.Food
                ? "FirstPersonFood"
                : "FirstPersonWater";
            activeItem.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            activeItem.transform.localScale = Vector3.one;

            foreach (Collider itemCollider in activeItem.GetComponentsInChildren<Collider>(true))
            {
                itemCollider.enabled = false;
            }

            foreach (Rigidbody itemBody in activeItem.GetComponentsInChildren<Rigidbody>(true))
            {
                itemBody.isKinematic = true;
                itemBody.detectCollisions = false;
            }

            foreach (Renderer itemVisual in activeItem.GetComponentsInChildren<Renderer>(true))
            {
                itemVisual.shadowCastingMode = ShadowCastingMode.Off;
                itemVisual.receiveShadows = false;
            }
        }

        private void SetItemPose(Vector3 localPosition, Vector3 localEuler, float scale)
        {
            if (visualRoot == null) return;

            visualRoot.localPosition = localPosition;
            visualRoot.localRotation = Quaternion.Euler(localEuler);
            visualRoot.localScale = Vector3.one * Mathf.Max(0f, scale);
        }

        private void HideItem()
        {
            if (activeItem != null)
            {
                activeItem.SetActive(false);
                activeItem.transform.SetParent(null, false);
                Destroy(activeItem);
                activeItem = null;
            }

            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(false);
            }
        }

        private void OnValidate()
        {
            raiseDuration = Mathf.Max(0.05f, raiseDuration);
            foodUseDuration = Mathf.Max(0.1f, foodUseDuration);
            waterUseDuration = Mathf.Max(0.1f, waterUseDuration);
            lowerDuration = Mathf.Max(0.05f, lowerDuration);
            combinedPause = Mathf.Max(0f, combinedPause);
            foodScale = Mathf.Max(0.05f, foodScale);
            waterScale = Mathf.Max(0.05f, waterScale);
            raiseArcHeight = Mathf.Max(0f, raiseArcHeight);
        }
    }
}
