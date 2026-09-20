using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LastBunkerEndingDebugButton : MonoBehaviour
    {
        private Button button;
        private CanvasGroup adminVisibility;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(PrepareEndingEve);
            RefreshAdminAccess();
        }

        private void Update()
        {
            RefreshAdminAccess();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(PrepareEndingEve);
        }

        public void PrepareEndingEve()
        {
            if (!GameSession.HasAdminAccess) return;

            ShelterEventDialogueController dialogue =
                FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            CharacterStats stats = FindFirstObjectByType<CharacterStats>(
                FindObjectsInactive.Include);

            if (dialogue == null)
            {
                Debug.LogError("마지막 벙커 이벤트 컨트롤러를 찾을 수 없습니다.", this);
                return;
            }

            if (stats != null)
            {
                stats.ModifyHealth(100f - stats.Health, "Ending debug restore");
                stats.ModifyHunger(100f - stats.Hunger);
                stats.ModifyThirst(100f - stats.Thirst);
                stats.ModifyMorale(100f - stats.Morale);
            }

            dialogue.SkipToLastBunkerEndingEveForTesting();
        }

        private void RefreshAdminAccess()
        {
            AdminDebugVisibility.Apply(gameObject, ref adminVisibility);
        }
    }
}
