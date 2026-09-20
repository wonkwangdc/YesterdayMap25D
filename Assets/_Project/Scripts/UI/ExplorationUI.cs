using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.CameraSystem;
using YesterdayMap.Exploration;

namespace YesterdayMap.UI
{
    public sealed class ExplorationUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text resultText;
        [SerializeField] private Toggle takeGearToggle;
        [SerializeField] private Button[] locationButtons;
        [SerializeField] private Text[] locationDetails;
        [SerializeField] private ExplorationLocationData[] locations;
        [SerializeField] private Button closeButton;
        [SerializeField] private ExplorationManager manager;

        public void Configure(GameObject root, Text result, Toggle gear, Button[] buttons, Text[] details,
            ExplorationLocationData[] locationData, Button close, ExplorationManager explorationManager)
        {
            panel = root; resultText = result; takeGearToggle = gear; locationButtons = buttons;
            locationDetails = details; locations = locationData; closeButton = close; manager = explorationManager;
        }

        private void Start()
        {
            for (int i = 0; i < locationButtons.Length && i < locations.Length; i++)
            {
                int index = i;
                locationButtons[i].onClick.AddListener(() => manager.StartExploration(locations[index], takeGearToggle.isOn));
            }
            closeButton.onClick.AddListener(Close);
            panel.SetActive(false);
        }

        private void Update()
        {
            if (panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open()
        {
            resultText.text = "장비를 사용하면 탐사 위험도가 감소합니다.";
            RefreshLocationDetails();
            panel.SetActive(true);
            IsometricCameraController.SetFirstPersonUiFocus(true);
        }
        public void Close()
        {
            panel.SetActive(false);
            IsometricCameraController.SetFirstPersonUiFocus(false);
        }
        public void ShowResult(string summary)
        {
            panel.SetActive(true);
            IsometricCameraController.SetFirstPersonUiFocus(true);
            resultText.text = summary;
            RefreshLocationDetails();
        }
        public void RefreshLocationDetails()
        {
            if (manager == null || locationDetails == null) return;
            for (int i = 0; i < locationDetails.Length && i < locations.Length; i++)
                locationDetails[i].text = manager.GetLocationStatus(locations[i]);
        }
    }
}
