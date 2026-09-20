using UnityEngine;

namespace YesterdayMap.Events
{
    [CreateAssetMenu(menuName = "Yesterday Map/Game Event")]
    public sealed class GameEventData : ScriptableObject
    {
        [SerializeField] private string eventName;
        [SerializeField, TextArea] private string message;
        [SerializeField, Range(0f, 1f)] private float baseChance = 0.2f;
        public string EventName => eventName;
        public string Message => message;
        public float BaseChance => baseChance;
    }
}
