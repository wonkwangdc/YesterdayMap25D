using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using YesterdayMap.Interaction;

namespace YesterdayMap.Prologue
{
    [RequireComponent(typeof(Collider))]
    // Temporary prologue exit point that reports inventory contents before scene transition is wired.
    public sealed class BunkerHatch : InteractableObject
    {
        [SerializeField, Min(0.2f)] private float interactionRange = 1.6f;
        [SerializeField] private UnityEvent onEnterBunker = new();

        private PlayerInventory nearbyInventory;
        private bool playerInRange;

        public override string InteractionPrompt => "E키: 벙커 입구 열기";
        public UnityEvent OnEnterBunker => onEnterBunker;

        private void Reset()
        {
            ConfigureTriggerCollider();
        }

        private void Awake()
        {
            ConfigureTriggerCollider();
        }

        private void Update()
        {
            if (!playerInRange)
            {
                return;
            }

            Debug.Log(InteractionPrompt);
            if (WasInteractPressed())
            {
                Interact();
            }
        }

        public override void Interact()
        {
            PlayerInventory inventory = nearbyInventory != null ? nearbyInventory : FindFirstObjectByType<PlayerInventory>();
            string inventoryText = inventory != null ? inventory.GetItemListText() : "No PlayerInventory found.";
            Debug.Log($"[BunkerHatch] Inventory before descent: {inventoryText}");
            Debug.Log("[BunkerHatch] 벙커로 내려갑니다");
            onEnterBunker.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null)
            {
                return;
            }

            nearbyInventory = inventory;
            playerInRange = true;
            Debug.Log(InteractionPrompt);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null || inventory != nearbyInventory)
            {
                return;
            }

            nearbyInventory = null;
            playerInRange = false;
        }

        private static bool WasInteractPressed()
        {
            if (Keyboard.current != null)
            {
                return Keyboard.current.eKey.wasPressedThisFrame;
            }

            return UnityEngine.Input.GetKeyDown(KeyCode.E);
        }

        private void ConfigureTriggerCollider()
        {
            Collider hatchCollider = GetComponent<Collider>();
            if (hatchCollider == null)
            {
                return;
            }

            hatchCollider.isTrigger = true;
            if (hatchCollider is BoxCollider boxCollider)
            {
                Vector3 scale = transform.lossyScale;
                boxCollider.size = new Vector3(
                    SafeLocalSize(interactionRange, scale.x),
                    SafeLocalSize(interactionRange, scale.y),
                    SafeLocalSize(interactionRange, scale.z));
            }
        }

        private static float SafeLocalSize(float worldSize, float axisScale)
        {
            return Mathf.Max(0.1f, worldSize / Mathf.Max(0.01f, Mathf.Abs(axisScale)));
        }
    }
}
