using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.BranchOne;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class Quarter3EndingEveDebugButton : MonoBehaviour
    {
        [SerializeField] private BranchRoute route;

        private Button button;
        private CanvasGroup adminVisibility;

        public void Configure(BranchRoute endingRoute)
        {
            route = endingRoute;
            ApplyLabel();
            RefreshAdminAccess();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(PrepareEndingEve);
            ApplyLabel();
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
            if (stats != null)
            {
                stats.ModifyHealth(
                    100f - stats.Health,
                    "Quarter 3 ending debug restore");
                stats.ModifyHunger(100f - stats.Hunger);
                stats.ModifyThirst(100f - stats.Thirst);
                stats.ModifyMorale(100f - stats.Morale);
            }

            if (dialogue == null ||
                !dialogue.SkipToQuarter3EndingEveForTesting(route))
            {
                Debug.LogError(
                    $"3분기 엔딩 전날 밤을 준비하지 못했습니다. Route: {route}",
                    this);
            }
        }

        private void ApplyLabel()
        {
            Text label = GetComponentInChildren<Text>(true);
            if (label == null) return;

            label.text = route == BranchRoute.Signal
                ? "[임시] 신호계열 엔딩 전날 밤"
                : "[임시] 합류계열 엔딩 전날 밤";
        }

        private void RefreshAdminAccess()
        {
            AdminDebugVisibility.Apply(gameObject, ref adminVisibility);
        }
    }
}
