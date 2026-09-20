using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LastSurvivalRecordDehydrationDebugButton : MonoBehaviour
    {
        private Button button;
        private CanvasGroup adminVisibility;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(PrepareDehydrationEndingEve);
            Text label = GetComponentInChildren<Text>(true);
            if (label != null) label.text = "[임시] 탈수 전날 밤";
            RefreshAdminAccess();
        }

        private void Update()
        {
            RefreshAdminAccess();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(PrepareDehydrationEndingEve);
        }

        public void PrepareDehydrationEndingEve()
        {
            if (!GameSession.HasAdminAccess) return;

            CharacterStats stats = FindFirstObjectByType<CharacterStats>(
                FindObjectsInactive.Include);
            if (stats == null ||
                !stats.PrepareNeedDeathEveForTesting("Dehydration"))
            {
                Debug.LogError(
                    "탈수 사망 전날 밤 상태를 준비하지 못했습니다.",
                    this);
                return;
            }

            Debug.Log(
                "[테스트] 탈수 사망 전날 밤입니다. 침대에서 잠을 자세요.",
                this);
        }

        private void RefreshAdminAccess()
        {
            AdminDebugVisibility.Apply(gameObject, ref adminVisibility);
        }
    }
}
