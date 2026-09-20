using UnityEngine;
using YesterdayMap.Resources;
using YesterdayMap.Scavenge;

namespace YesterdayMap.Core
{
    // 씬이 바뀌어도 Scavenge에서 챙긴 물자를 Shelter까지 전달한다.
    public sealed class GameSession : MonoBehaviour
    {
        public const int MaxPlayerNameLength = 8;
        public const string DefaultPlayerName = "생존자";
        public const string AdminPlayerName = "선웅기수재형";

        public static GameSession Instance { get; private set; }
        private readonly int[] collected = new int[6];
        private readonly int[] collectedItemKinds =
            new int[System.Enum.GetValues(typeof(ScavengeItemKind)).Length];
        private readonly int[] lastMoraleItemUseDays =
            new int[System.Enum.GetValues(typeof(ScavengeItemKind)).Length];
        private float elapsedPlaySeconds;
        private int foodConsumedCount;
        private int waterConsumedCount;
        [SerializeField] private string playerName = DefaultPlayerName;

        public float ElapsedPlaySeconds => elapsedPlaySeconds;
        public int FoodConsumedCount => foodConsumedCount;
        public int WaterConsumedCount => waterConsumedCount;
        public string PlayerName => string.IsNullOrWhiteSpace(playerName)
            ? DefaultPlayerName
            : playerName;
        public static string CurrentPlayerName => Instance != null
            ? Instance.PlayerName
            : DefaultPlayerName;
        public static bool HasAdminAccess => string.Equals(
            CurrentPlayerName,
            AdminPlayerName,
            System.StringComparison.Ordinal);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            elapsedPlaySeconds += Time.unscaledDeltaTime;
        }

        public void RecordFoodConsumed() => foodConsumedCount++;
        public void RecordWaterConsumed() => waterConsumedCount++;

        public static bool TrySetPlayerName(string value)
        {
            string normalized = NormalizePlayerName(value);
            if (normalized.Length == 0) return false;
            if (Instance == null)
            {
                new GameObject("GameSession").AddComponent<GameSession>();
            }
            Instance.playerName = normalized;
            return true;
        }

        public void ResetSession()
        {
            for (int i = 0; i < collected.Length; i++) collected[i] = 0;
            for (int i = 0; i < collectedItemKinds.Length; i++) collectedItemKinds[i] = 0;
            for (int i = 0; i < lastMoraleItemUseDays.Length; i++)
                lastMoraleItemUseDays[i] = -1;
            elapsedPlaySeconds = 0f;
            foodConsumedCount = 0;
            waterConsumedCount = 0;
            playerName = string.Empty;
        }

        public void Collect(ResourceType type, int amount)
        {
            Collect(type, amount, ScavengeItemKind.Resource);
        }

        public void Collect(ResourceType type, int amount, ScavengeItemKind itemKind)
        {
            amount = Mathf.Max(0, amount);
            if (itemKind != ScavengeItemKind.Resource)
            {
                int itemKindIndex = (int)itemKind;
                if (itemKindIndex >= 0 && itemKindIndex < collectedItemKinds.Length)
                    collectedItemKinds[itemKindIndex] += amount > 0 ? 1 : 0;
                return;
            }

            int index = (int)type;
            if (index >= 0 && index < collected.Length) collected[index] += amount;
        }

        public int GetCollected(ResourceType type)
        {
            int index = (int)type;
            return index >= 0 && index < collected.Length ? collected[index] : 0;
        }

        public int GetCollected(ScavengeItemKind itemKind)
        {
            int index = (int)itemKind;
            return index >= 0 && index < collectedItemKinds.Length ? collectedItemKinds[index] : 0;
        }

        public bool CanUseMoraleItem(ScavengeItemKind itemKind, int currentDay)
        {
            int index = (int)itemKind;
            return IsMoraleItem(itemKind) &&
                   currentDay > 0 &&
                   GetCollected(itemKind) > 0 &&
                   index >= 0 &&
                   index < lastMoraleItemUseDays.Length &&
                   lastMoraleItemUseDays[index] != currentDay;
        }

        public bool TryUseMoraleItem(ScavengeItemKind itemKind, int currentDay)
        {
            if (!CanUseMoraleItem(itemKind, currentDay)) return false;
            lastMoraleItemUseDays[(int)itemKind] = currentDay;
            return true;
        }

        public void TransferCollectedTo(ResourceManager manager)
        {
            if (manager == null) return;

            for (int i = 0; i < collected.Length; i++)
            {
                if (collected[i] > 0) manager.Add((ResourceType)i, collected[i]);
                collected[i] = 0;
            }
        }

        public GameSessionSaveData CaptureState()
        {
            return new GameSessionSaveData
            {
                playerName = PlayerName,
                collectedResources = (int[])collected.Clone(),
                collectedItems = (int[])collectedItemKinds.Clone(),
                lastMoraleItemUseDays = (int[])lastMoraleItemUseDays.Clone(),
                elapsedPlaySeconds = elapsedPlaySeconds,
                foodConsumedCount = foodConsumedCount,
                waterConsumedCount = waterConsumedCount
            };
        }

        public void RestoreState(GameSessionSaveData data)
        {
            ResetSession();
            if (data == null) return;

            CopyInto(data.collectedResources, collected);
            CopyInto(data.collectedItems, collectedItemKinds);
            CopyInto(data.lastMoraleItemUseDays, lastMoraleItemUseDays, false);
            playerName = NormalizePlayerName(data.playerName);
            if (playerName.Length == 0) playerName = DefaultPlayerName;
            elapsedPlaySeconds = Mathf.Max(0f, data.elapsedPlaySeconds);
            foodConsumedCount = Mathf.Max(0, data.foodConsumedCount);
            waterConsumedCount = Mathf.Max(0, data.waterConsumedCount);
        }

        private static bool IsMoraleItem(ScavengeItemKind itemKind)
        {
            return itemKind == ScavengeItemKind.TeddyBear ||
                   itemKind == ScavengeItemKind.SoccerBall ||
                   itemKind == ScavengeItemKind.Guitar;
        }

        private static string NormalizePlayerName(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
            return normalized.Length <= MaxPlayerNameLength
                ? normalized
                : normalized.Substring(0, MaxPlayerNameLength);
        }

        private static void CopyInto(
            int[] source,
            int[] destination,
            bool clampNonnegative = true)
        {
            if (source == null || destination == null) return;
            int count = Mathf.Min(source.Length, destination.Length);
            for (int i = 0; i < count; i++)
                destination[i] = clampNonnegative
                    ? Mathf.Max(0, source[i])
                    : source[i];
        }
    }
}
