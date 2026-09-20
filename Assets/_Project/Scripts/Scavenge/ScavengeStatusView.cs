using System;
using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.Scavenge
{
    /// <summary>
    /// Owns the editable Scavenge status visuals without containing game rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScavengeStatusView : MonoBehaviour
    {
        [Header("Deposited Items")]
        [SerializeField] private GameObject depositedSummaryRoot;
        [SerializeField] private Text depositedSummaryText;
        [Header("Time Expiry")]
        [SerializeField] private Image darknessOverlay;
        [SerializeField] private GameObject gameOverContent;
        [SerializeField] private Button returnToMainMenuButton;

        private Action returnToMainMenu;
        private Material runtimeDarknessMaterial;

        public void Initialize(Action returnAction)
        {
            returnToMainMenu = returnAction;
            if (returnToMainMenuButton != null)
            {
                returnToMainMenuButton.onClick.RemoveListener(HandleReturnRequested);
                returnToMainMenuButton.onClick.AddListener(HandleReturnRequested);
            }

            CreateDarknessMaterial();
            SetDepositedSummary(string.Empty, false);
            if (gameOverContent != null) gameOverContent.SetActive(false);
            if (darknessOverlay != null) darknessOverlay.raycastTarget = false;
        }

        public void SetDepositedSummary(string summary, bool visible)
        {
            if (depositedSummaryText != null)
                depositedSummaryText.text = summary ?? string.Empty;
            if (depositedSummaryRoot != null)
                depositedSummaryRoot.SetActive(visible);
        }

        public void SetDarkness(float progress, float radius)
        {
            if (darknessOverlay == null) return;

            float clampedProgress = Mathf.Clamp01(progress);
            if (runtimeDarknessMaterial != null)
            {
                runtimeDarknessMaterial.SetFloat("_Radius", radius);
                runtimeDarknessMaterial.SetFloat("_Darkness", clampedProgress);
                return;
            }

            Color color = darknessOverlay.color;
            color.a = clampedProgress;
            darknessOverlay.color = color;
        }

        public void SetDarknessCenter(Vector2 center)
        {
            if (runtimeDarknessMaterial != null)
                runtimeDarknessMaterial.SetVector("_Center", center);
        }

        public void RevealGameOver()
        {
            if (gameOverContent != null) gameOverContent.SetActive(true);
            if (darknessOverlay != null) darknessOverlay.raycastTarget = true;
        }

        /// <summary>
        /// Used only by the editor installer when creating the prefab.
        /// </summary>
        public void Configure(
            GameObject summaryRoot,
            Text summaryText,
            Image overlay,
            GameObject content,
            Button returnButton)
        {
            depositedSummaryRoot = summaryRoot;
            depositedSummaryText = summaryText;
            darknessOverlay = overlay;
            gameOverContent = content;
            returnToMainMenuButton = returnButton;
        }

        private void CreateDarknessMaterial()
        {
            if (darknessOverlay == null || runtimeDarknessMaterial != null) return;

            Shader darknessShader = Shader.Find("YesterdayMap/UI/CircularDarkness");
            if (darknessShader == null)
            {
                Debug.LogError("CircularDarkness shader was not found. Falling back to a plain fade.", this);
                darknessOverlay.color = new Color(0f, 0f, 0f, 0f);
                return;
            }

            runtimeDarknessMaterial = new Material(darknessShader)
            {
                name = "CircularDarkness (Runtime)"
            };
            darknessOverlay.material = runtimeDarknessMaterial;
        }

        private void HandleReturnRequested()
        {
            returnToMainMenu?.Invoke();
        }

        private void OnDestroy()
        {
            if (returnToMainMenuButton != null)
                returnToMainMenuButton.onClick.RemoveListener(HandleReturnRequested);
            if (runtimeDarknessMaterial != null)
                Destroy(runtimeDarknessMaterial);
        }
    }
}
