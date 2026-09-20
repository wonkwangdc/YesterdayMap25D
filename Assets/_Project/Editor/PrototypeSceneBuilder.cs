#if UNITY_EDITOR
using UnityEditor;

namespace YesterdayMap.Editor
{
    // 기존 메뉴와 자동화 명령을 유지하면서 v0.2 빌더로 연결한다.
    public static class PrototypeSceneBuilder
    {
        [MenuItem("Yesterday Map/Build Playable Prototype")]
        public static void Build() => V02ProjectBuilder.BuildAll();
        public static void BuildFromCommandLine() { V02ProjectBuilder.BuildAll(); EditorApplication.Exit(0); }
    }
}
#endif
