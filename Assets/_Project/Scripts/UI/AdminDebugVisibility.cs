using UnityEngine;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    // 개발용 UI의 표시와 입력을 플레이어 이름 권한에 맞춰 함께 제어한다.
    internal static class AdminDebugVisibility
    {
        public static bool Apply(GameObject target, ref CanvasGroup group)
        {
            if (target == null)
            {
                return false;
            }

            if (group == null)
            {
                group = target.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = target.AddComponent<CanvasGroup>();
                }
            }

            bool visible = GameSession.HasAdminAccess;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            return visible;
        }
    }
}
