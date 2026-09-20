using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Shelter;

namespace YesterdayMap.UI
{
    // View for choosing which broken shelter facility to repair.
    public sealed class WorkbenchRepairUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text statusText;
        [SerializeField] private Button purifierButton;
        [SerializeField] private Button generatorButton;
        [SerializeField] private Button barricadeButton;
        [SerializeField] private Button closeButton;

        private WorkbenchObject workbench;

        private void Start()
        {
            BindButtons();
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            purifierButton?.onClick.RemoveListener(RepairPurifier);
            generatorButton?.onClick.RemoveListener(RepairGenerator);
            barricadeButton?.onClick.RemoveListener(RepairBarricade);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (panel != null &&
                panel.activeSelf &&
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(WorkbenchObject source)
        {
            if (panel == null || source == null) return;
            workbench = source;
            if (statusText != null) statusText.text = "수리할 시설을 선택하세요.";
            Refresh();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            workbench = null;
        }

        private void RepairPurifier()
        {
            Repair(ShelterRepairTarget.WaterPurifier);
        }

        private void RepairGenerator()
        {
            Repair(ShelterRepairTarget.Generator);
        }

        private void RepairBarricade()
        {
            Repair(ShelterRepairTarget.Barricade);
        }

        private void Repair(ShelterRepairTarget target)
        {
            if (workbench == null) return;
            workbench.TryRepair(target, out string message);
            if (statusText != null) statusText.text = message;
            Refresh();
        }

        private void Refresh()
        {
            ConfigureFacilityButton(
                purifierButton,
                ShelterRepairTarget.WaterPurifier);
            ConfigureFacilityButton(
                generatorButton,
                ShelterRepairTarget.Generator);
            ConfigureFacilityButton(
                barricadeButton,
                ShelterRepairTarget.Barricade);
        }

        private void ConfigureFacilityButton(
            Button button,
            ShelterRepairTarget target)
        {
            if (button == null || workbench == null) return;

            ShelterObject facility = workbench.GetFacility(target);
            bool isBroken = facility != null && facility.IsBroken;
            int cost = workbench.GetRepairCost(target);
            string state = facility == null
                ? "연결 필요"
                : isBroken
                    ? $"부품 {cost}개"
                    : "정상";

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = $"{workbench.GetRepairLabel(target)}  ·  {state}";
            }

            button.interactable = isBroken && workbench.HasPartsFor(target);
        }

        private void BindButtons()
        {
            purifierButton?.onClick.RemoveListener(RepairPurifier);
            purifierButton?.onClick.AddListener(RepairPurifier);
            generatorButton?.onClick.RemoveListener(RepairGenerator);
            generatorButton?.onClick.AddListener(RepairGenerator);
            barricadeButton?.onClick.RemoveListener(RepairBarricade);
            barricadeButton?.onClick.AddListener(RepairBarricade);
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
        }
    }
}
