using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    // 프로젝트 기본 SUIT 글꼴을 적용하되 고유 서체를 쓰는 일기장과 탐사 씬은 제외한다.
    public sealed class KoreanFontApplicator : MonoBehaviour
    {
        private bool applying;

        private void Awake() => ApplyDefaultFont();
        private void OnEnable() => ApplyDefaultFont();
        private void Start() => ApplyDefaultFont();
        private void OnTransformChildrenChanged() => ApplyDefaultFont();

        private void ApplyDefaultFont()
        {
            if (applying || gameObject.scene.name == "Exploration") return;
            Font defaultFont = DefaultUIFont.Get();
            if (defaultFont == null) return;

            applying = true;
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (!DefaultUIFont.ShouldExclude(text))
                    text.font = defaultFont;
            }
            applying = false;
        }
    }
}
