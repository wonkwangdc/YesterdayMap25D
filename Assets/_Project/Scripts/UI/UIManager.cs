using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    // Displays Shelter UI state and forwards player UI actions without owning game rules.
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private Text messageText;
        [SerializeField] private Button endDayButton;
        [SerializeField] private StorageUI storageUI;
        [SerializeField] private ExplorationUI explorationUI;
        [SerializeField] private WorkProgressUI workProgressUI;
        [SerializeField, Min(0.1f)] private float messageDuration = 3f;

        private Coroutine hideMessageRoutine;

        public string CurrentMessage { get; private set; } = string.Empty;

        public void Configure(
            DayCycleManager cycle,
            CharacterStats characterStats,
            Text message,
            Button endDay,
            StorageUI storage,
            ExplorationUI exploration,
            WorkProgressUI workProgress)
        {
            dayCycle = cycle;
            stats = characterStats;
            messageText = message;
            endDayButton = endDay;
            storageUI = storage;
            explorationUI = exploration;
            workProgressUI = workProgress;
        }

        private void Start()
        {
            if (endDayButton != null)
            {
                endDayButton.onClick.RemoveAllListeners();
                endDayButton.gameObject.SetActive(false);
            }

            Refresh();
            ShowMessage("시설은 가까이 다가가 E키로 사용합니다. 하루를 넘기려면 침대에서 잠을 자야 합니다.");
        }

        private void OnEnable()
        {
            if (stats != null) stats.WarningRaised += ShowMessage;
        }

        private void OnDisable()
        {
            if (stats != null) stats.WarningRaised -= ShowMessage;

            if (hideMessageRoutine != null)
            {
                StopCoroutine(hideMessageRoutine);
                hideMessageRoutine = null;
            }
        }

        public void Refresh()
        {
            // 날짜와 자원 정보는 일기장에서 확인한다.
        }

        public void ShowMessage(string message)
        {
            CurrentMessage = message ?? string.Empty;
            if (messageText == null) return;

            if (hideMessageRoutine != null)
            {
                StopCoroutine(hideMessageRoutine);
                hideMessageRoutine = null;
            }

            messageText.text = CurrentMessage;
            messageText.enabled = !string.IsNullOrEmpty(CurrentMessage);
            if (messageText.enabled && isActiveAndEnabled)
            {
                hideMessageRoutine = StartCoroutine(HideMessageAfterDelay());
            }
        }

        private IEnumerator HideMessageAfterDelay()
        {
            float visibleElapsed = 0f;
            while (visibleElapsed < messageDuration)
            {
                yield return null;
                if (messageText == null ||
                    messageText.gameObject.activeInHierarchy)
                {
                    visibleElapsed += Time.unscaledDeltaTime;
                }
            }

            CurrentMessage = string.Empty;
            if (messageText != null)
            {
                messageText.text = string.Empty;
                messageText.enabled = false;
            }

            hideMessageRoutine = null;
        }

        public void OpenStorage() => storageUI?.Open();
        public void OpenExploration() => explorationUI?.Open();

        public bool StartWork(string label, float duration, System.Action completed)
        {
            return workProgressUI != null && workProgressUI.Begin(label, duration, completed);
        }
    }
}
