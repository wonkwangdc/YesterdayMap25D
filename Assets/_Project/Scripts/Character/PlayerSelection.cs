using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace YesterdayMap.Character
{
    public sealed class PlayerSelection : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera worldCamera;
        [SerializeField] private GameObject selectionMarker;
        public bool IsSelected { get; private set; } = true;

        public void Configure(UnityEngine.Camera camera, GameObject marker)
        { worldCamera = camera; selectionMarker = marker; SetSelected(true); }

        public void SetSelectionVisible(bool visible) => SetSelected(visible);

        private void Awake()
        {
            ResolveCamera();
        }

        private void Update()
        {
            ResolveCamera();
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (worldCamera == null) return;
            Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.transform.IsChildOf(transform)) SetSelected(true);
        }

        private void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionMarker != null) selectionMarker.SetActive(selected);
        }

        private void ResolveCamera()
        {
            if (worldCamera == null) worldCamera = UnityEngine.Camera.main;
        }
    }
}
