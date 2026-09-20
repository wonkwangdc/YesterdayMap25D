using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    [CreateAssetMenu(menuName = "Yesterday Map/UI/Default Font Settings")]
    public sealed class DefaultUIFontSettings : ScriptableObject
    {
        [SerializeField] private Font defaultFont;

        public Font DefaultFont => defaultFont;
    }

    public static class DefaultUIFont
    {
        private const string SettingsResourcePath = "UI/DefaultUIFontSettings";
        private static Font cachedFont;
        private static float nextRuntimeScanTime;

        public static Font Get()
        {
            if (cachedFont != null) return cachedFont;
            DefaultUIFontSettings settings =
                UnityEngine.Resources.Load<DefaultUIFontSettings>(SettingsResourcePath);
            cachedFont = settings != null ? settings.DefaultFont : null;
            return cachedFont;
        }

        public static bool ShouldExclude(Text text)
        {
            if (text == null || text.gameObject.scene.name == "Exploration")
                return true;

            for (Transform current = text.transform; current != null; current = current.parent)
            {
                if (current.name.IndexOf("Diary", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallRuntimeDefault()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Canvas.willRenderCanvases -= ApplyToLoadedTexts;
            Canvas.willRenderCanvases += ApplyToLoadedTexts;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
            ApplyToLoadedTexts();

        private static void ApplyToLoadedTexts()
        {
            if (Application.isPlaying && Time.unscaledTime < nextRuntimeScanTime)
                return;
            nextRuntimeScanTime = Time.unscaledTime + 0.25f;

            Font defaultFont = Get();
            if (defaultFont == null) return;

            Text[] texts = Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Text text in texts)
            {
                if (!ShouldExclude(text) && text.font != defaultFont)
                    text.font = defaultFont;
            }
        }
    }
}
