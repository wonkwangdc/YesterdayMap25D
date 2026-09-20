using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        public void Configure(Text text) => promptText = text;
        public void SetPrompt(string prompt)
        {
            if (promptText == null) return;
            promptText.text = prompt;
            promptText.enabled = !string.IsNullOrEmpty(prompt);
        }
    }
}
