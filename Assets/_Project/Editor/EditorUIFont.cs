#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace YesterdayMap.EditorTools
{
    internal static class EditorUIFont
    {
        internal const string AssetPath =
            "Assets/_Project/Scenes/font/SUIT-Regular.otf";

        internal static Font Default =>
            AssetDatabase.LoadAssetAtPath<Font>(AssetPath);
    }
}
#endif
