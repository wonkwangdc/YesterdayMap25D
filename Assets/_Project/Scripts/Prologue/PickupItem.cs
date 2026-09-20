using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using YesterdayMap.Interaction;

namespace YesterdayMap.Prologue
{
    [RequireComponent(typeof(Collider))]
    // World pickup that can work with the existing InteractableObject flow or direct trigger range checks.
    public sealed class PickupItem : InteractableObject
    {
        [SerializeField] private ItemData itemData;
        [SerializeField, Min(0.2f)] private float interactionRange = 1.4f;
        [SerializeField] private UnityEvent<ItemData> onPickedUp = new();

        private PlayerInventory nearbyInventory;
        private bool playerInRange;

        public override string InteractionPrompt => itemData != null ? $"E키: {itemData.DisplayName} 챙기기" : "E키: 아이템 챙기기";
        public UnityEvent<ItemData> OnPickedUp => onPickedUp;

        private void Reset()
        {
            // Trigger colliders make range detection independent from the existing click interaction flow.
            ConfigureTriggerCollider();
        }

        private void Awake()
        {
            ConfigureTriggerCollider();
        }

        private void Update()
        {
            if (!playerInRange || itemData == null)
            {
                return;
            }

            Debug.Log(InteractionPrompt);
            if (WasInteractPressed())
            {
                Interact();
            }
        }

        public void Configure(ItemData data)
        {
            itemData = data;
            SetPrompt(InteractionPrompt);
        }

        public override void Interact()
        {
            if (itemData == null)
            {
                Debug.LogWarning("[PickupItem] Pickup failed because item data is missing.");
                return;
            }

            PlayerInventory inventory = nearbyInventory != null ? nearbyInventory : FindFirstObjectByType<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning($"[PickupItem] No PlayerInventory found for {itemData.DisplayName}.");
                return;
            }

            if (!inventory.TryAddItem(itemData))
            {
                Debug.Log($"[PickupItem] Inventory is full. Could not pick up {itemData.DisplayName}.");
                return;
            }

            onPickedUp.Invoke(itemData);
            Debug.Log($"[PickupItem] Picked up {itemData.DisplayName} ({itemData.ItemId}).");
            gameObject.SetActive(false);
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
            Collider itemCollider = GetComponent<Collider>();
            if (itemCollider == null)
            {
                return;
            }

            itemCollider.isTrigger = true;
            if (itemCollider is BoxCollider boxCollider)
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
