using System;
using UnityEngine;
using YesterdayMap.Resources;

namespace YesterdayMap.Exploration
{
    public enum OccupationType { Empty, Zombies, Survivors, Military, Raiders }
    // 장소와 보상표를 코드 밖 에셋으로 분리해 새 탐사지를 쉽게 추가할 수 있다.
    [CreateAssetMenu(menuName = "Yesterday Map/Exploration Location")]
    public sealed class ExplorationLocationData : ScriptableObject
    {
        [Serializable]
        public struct AmountChance
        {
            [Min(0)] public int amount;
            [Min(0f)] public float weight;
        }

        [Serializable]
        public struct RewardEntry
        {
            public ResourceType type;
            [Min(0)] public int minAmount;
            [Min(0)] public int maxAmount;
            [Min(0f)] public float selectionWeight;
            public AmountChance[] amountChances;
            public AmountChance[] equippedAmountChances;
        }

        [SerializeField] private string locationName;
        [SerializeField] private string expectedRewards;
        [SerializeField, Range(0f, 1f)] private float dangerChance = 0.2f;
        [SerializeField, Range(1, 4)] private int dangerStars = 2;
        [SerializeField] private RewardEntry[] rewards;

        public string LocationName => locationName;
        public float DangerChance => dangerChance;
        public int DangerStars => Mathf.Clamp(dangerStars, 1, 4);
        public string ExpectedRewards => expectedRewards;
        public RewardEntry[] Rewards => rewards;

        public void Configure(string displayName, float danger, RewardEntry[] rewardTable,
            string rewardSummary = "", int starRating = 2)
        {
            locationName = displayName;
            dangerChance = danger;
            dangerStars = Mathf.Clamp(starRating, 1, 4);
            rewards = rewardTable;
            expectedRewards = rewardSummary;
        }
    }
}
