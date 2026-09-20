using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.Resources;
using YesterdayMap.UI;

namespace YesterdayMap.Exploration
{
    public sealed class ExplorationManager : MonoBehaviour
    {
        private const float DualRewardChance = 0.10f;
        private const float EquippedDualRewardChance = 0.20f;
        private const float EquippedFoodWaterBonusChance = 0.05f;

        [SerializeField] private ResourceManager resources;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private ExplorationUI explorationUI;
        [SerializeField] private ExplorationLocationData[] locations;
        private ShelterEventDialogueController eventDialogueController;
        private readonly Dictionary<ExplorationLocationData, float> dailyRisk = new();
        private readonly Dictionary<ExplorationLocationData, OccupationType> occupation = new();
        private readonly List<string> explorationHistory = new();
        private int lastExplorationDay = -1;

        public IReadOnlyList<string> ExplorationHistory => explorationHistory;
        public int CurrentDay => dayCycle != null ? dayCycle.CurrentDay : 1;
        public event System.Action<string> ResultResolved;
        public event System.Action ReturnedToShelter;
        public bool HasExploredToday =>
            dayCycle != null && lastExplorationDay == dayCycle.CurrentDay;
        public bool CanExploreToday
        {
            get
            {
                ResolveEventDialogueController();
                return dayCycle != null &&
                       dayCycle.CurrentDay >= 2 &&
                       lastExplorationDay != dayCycle.CurrentDay &&
                       (stats == null || stats.CanExplore) &&
                       (eventDialogueController == null ||
                        (!eventDialogueController
                             .IsQuarter3SignalStoryPendingBeforeExploration &&
                         !eventDialogueController.IsQuarter3FinalChoiceDay));
            }
        }

        public string ExplorationBlockMessage
        {
            get
            {
                ResolveEventDialogueController();
                if (dayCycle != null && dayCycle.CurrentDay < 2)
                {
                    return "밖은 아직 위험하다. 오늘은 벙커 안에서 상황을 정리하는 편이 좋을 것 같다.";
                }

                if (eventDialogueController != null &&
                    eventDialogueController
                        .IsQuarter3SignalStoryPendingBeforeExploration)
                {
                    return "스토리 먼저 확인하세요";
                }

                if (eventDialogueController != null &&
                    eventDialogueController.IsQuarter3FinalChoiceDay)
                {
                    return "오늘은 새로운 탐사를 진행할 수 없습니다. 마지막 방송과 기록을 확인한 뒤 최종 선택하세요.";
                }

                return stats != null && !stats.CanExplore
                    ? "정신력이 0이라 탐사를 진행할 수 없습니다."
                    : "오늘은 이미 탐사를 다녀왔습니다.";
            }
        }

        public void Configure(ResourceManager resourceManager, CharacterStats characterStats,
            DayCycleManager cycle, ExplorationUI ui, ExplorationLocationData[] locationData)
        {
            resources = resourceManager;
            stats = characterStats;
            dayCycle = cycle;
            explorationUI = ui;
            locations = locationData;
        }

        private void Start()
        {
            if (dayCycle != null) dayCycle.DayChanged += RerollDailyConditions;
            RerollDailyConditions();
        }

        private void OnDestroy()
        {
            if (dayCycle != null) dayCycle.DayChanged -= RerollDailyConditions;
        }

        public string GetLocationStatus(ExplorationLocationData location)
        {
            float risk = dailyRisk.TryGetValue(location, out float value)
                ? value
                : BaseDangerForStars(location.DangerStars);
            OccupationType current = occupation.TryGetValue(location, out OccupationType type)
                ? type
                : OccupationType.Empty;
            return $"{location.LocationName}\n예상 보상: {location.ExpectedRewards}\n" +
                   $"오늘의 위험도: {RiskKorean(risk)}\n점령 세력: {OccupationKorean(current)}";
        }

        public static string GetLocationMemo(ExplorationLocationData location)
        {
            if (location == null) return "탐사 장소를 선택하세요.";
            int filledStars = location.DangerStars;
            string stars = new string('★', filledStars) + new string('☆', 4 - filledStars);
            string rewards = location.ExpectedRewards.Replace(", ", " · ");
            return $"위험도 {stars}  {DangerGradeKorean(filledStars)}\n획득 가능 물품:\n{rewards}";
        }

        public string GetLocationMemoForCurrentState(
            ExplorationLocationData location)
        {
            string memo = GetLocationMemo(location);
            ResolveEventDialogueController();
            if (eventDialogueController == null ||
                !eventDialogueController.TryGetQuarter3SignalExplorationProgress(
                    location?.LocationName,
                    out var progress))
            {
                return memo;
            }

            if (progress.RemainingCount > 0)
            {
                memo += "\n누군가의 일지";
            }

            memo +=
                $"\n판단 기록: {progress.AcquiredCount}/{progress.TotalCount}";
            if (progress.RemainingCount == 0)
            {
                memo += " · 확인 완료";
            }

            return memo;
        }

        public ExplorationLocationData GetLocation(int index) => locations[Mathf.Clamp(index, 0, locations.Length - 1)];

        public ExplorationSaveData CaptureState() => new()
        {
            lastExplorationDay = lastExplorationDay,
            history = explorationHistory.ToArray()
        };

        public void RestoreState(ExplorationSaveData data)
        {
            if (data == null) return;
            lastExplorationDay = data.lastExplorationDay;
            explorationHistory.Clear();
            if (data.history != null) explorationHistory.AddRange(data.history);
            explorationUI?.RefreshLocationDetails();
        }

        public bool StartExploration(ExplorationLocationData location, bool takeGear)
        {
            if (location == null || !CanExploreToday) return false;
            ExplorationLocationData.RewardEntry[] rewardTable = location.Rewards;
            if (rewardTable == null || rewardTable.Length == 0)
            {
                Debug.LogError($"[ExplorationManager] {location.name} has no exploration rewards configured.");
                return false;
            }

            lastExplorationDay = dayCycle.CurrentDay;
            if (stats != null)
            {
                stats.ModifyHunger(-15f);
                stats.ModifyThirst(-10f);
            }

            float risk = dailyRisk.TryGetValue(location, out float rolledRisk)
                ? rolledRisk
                : BaseDangerForStars(location.DangerStars);
            bool critical = stats != null && stats.IsCriticalCondition;
            float danger = Mathf.Clamp01(
                risk + (critical ? 0.15f : 0f) - (takeGear ? 0.15f : 0f));
            string summary;

            bool explorationSucceeded = Random.value >= danger;
            if (!explorationSucceeded)
            {
                float outcome = Random.value;
                if (outcome < 0.25f)
                {
                    float damage = takeGear ? 8f : 18f;
                    stats.ModifyHealth(-damage, "Exploration injury");
                    summary = $"{location.LocationName} 탐사 중 부상을 입었다. 체력이 {damage:0}만큼 줄었다.";
                }
                else if (outcome < 0.50f)
                {
                    float moraleLoss = takeGear ? 6f : 12f;
                    stats.ModifyMorale(-moraleLoss);
                    summary = $"{location.LocationName}에서 위험을 피해 돌아왔지만 정신력이 {moraleLoss:0}만큼 줄었다.";
                }
                else
                {
                    summary = $"{location.LocationName} 탐사에서는 쓸 만한 물건을 발견하지 못했다.";
                }
            }
            else
            {
                ExplorationLocationData.RewardEntry reward = RollReward(rewardTable);
                int amount = RollRewardAmount(
                    reward, takeGear, critical, out bool equipmentBonusGranted);
                resources.Add(reward.type, amount);
                summary = $"{location.LocationName} 탐사에서 {ExplorationRewardKorean(reward.type)} {amount}개를 챙겨왔다.";

                bool secondEquipmentBonusGranted = false;
                ResourceType secondRewardType = default;
                float dualRewardChance = takeGear
                    ? EquippedDualRewardChance
                    : DualRewardChance;
                if (Random.value < dualRewardChance &&
                    TryRollDifferentReward(rewardTable, reward.type, out var secondReward))
                {
                    int secondAmount = RollRewardAmount(
                        secondReward,
                        takeGear,
                        critical,
                        out secondEquipmentBonusGranted);
                    secondRewardType = secondReward.type;
                    resources.Add(secondReward.type, secondAmount);
                    summary +=
                        $"\n{ExplorationRewardKorean(secondReward.type)} {secondAmount}개도 함께 챙겨왔다.";
                }

                if (equipmentBonusGranted || secondEquipmentBonusGranted)
                {
                    string bonusItems = equipmentBonusGranted
                        ? $"{ExplorationRewardKorean(reward.type)} +1"
                        : string.Empty;
                    if (secondEquipmentBonusGranted)
                    {
                        if (bonusItems.Length > 0) bonusItems += "\n";
                        bonusItems += $"{ExplorationRewardKorean(secondRewardType)} +1";
                    }

                    summary +=
                        "\n\n<b>역시 빈손으로 나가는 것보단 낫다.</b>\n" +
                        "뜻밖에 먹을 것과 물을 조금 더 챙겨왔다.\n" +
                        bonusItems;
                }
            }

            ResolveEventDialogueController();
            if (eventDialogueController != null &&
                eventDialogueController.TryGetLastBunkerExplorationResultAppend(
                    out string lastBunkerResultAppend))
            {
                summary += $"\n\n{lastBunkerResultAppend}";
            }

            if (explorationSucceeded &&
                eventDialogueController != null &&
                eventDialogueController.TryAcquireQuarter3SignalExplorationClue(
                    location.LocationName,
                    out string acquiredTitle))
            {
                summary += $"\n\n누군가의 일지를 얻었다.\n「{acquiredTitle}」";
            }

            explorationHistory.Add($"{dayCycle.CurrentDay}일차 · {summary}");
            if (ResultResolved != null) ResultResolved.Invoke(summary);
            else explorationUI?.ShowResult(summary);
            return true;
        }

        public void NotifyReturnedToShelter()
        {
            ReturnedToShelter?.Invoke();
        }

        private void ResolveEventDialogueController()
        {
            if (eventDialogueController == null)
            {
                eventDialogueController =
                    FindFirstObjectByType<ShelterEventDialogueController>(
                        FindObjectsInactive.Include);
            }
        }

        private void RerollDailyConditions()
        {
            dailyRisk.Clear();
            occupation.Clear();
            if (locations == null) return;
            foreach (ExplorationLocationData location in locations)
            {
                OccupationType type = (OccupationType)Random.Range(0, 5);
                float modifier = type switch
                {
                    OccupationType.Zombies => 0.2f,
                    OccupationType.Raiders => 0.16f,
                    OccupationType.Military => -0.08f,
                    OccupationType.Survivors => -0.05f,
                    _ => 0f
                };
                dailyRisk[location] = Mathf.Clamp(
                    BaseDangerForStars(location.DangerStars) + modifier +
                    Random.Range(-0.08f, 0.09f), 0.05f, 0.9f);
                occupation[location] = type;
            }
            explorationUI?.RefreshLocationDetails();
        }

        private static string DangerGradeKorean(int stars) => stars switch
        {
            1 => "낮음", 2 => "보통", 3 => "높음", _ => "최상"
        };
        private static string RiskKorean(float risk) => risk < 0.28f ? "낮음" : risk < 0.55f ? "보통" : "높음";
        private static string OccupationKorean(OccupationType type) => type switch
        {
            OccupationType.Zombies => "좀비 무리",
            OccupationType.Survivors => "생존자",
            OccupationType.Military => "군인",
            OccupationType.Raiders => "약탈자",
            _ => "비어 있음"
        };
        private static string ExplorationRewardKorean(ResourceType type) => type switch
        {
            ResourceType.Food => "식량",
            ResourceType.Water => "식수",
            ResourceType.Medicine => "약품",
            ResourceType.Parts => "수리키트",
            ResourceType.Battery => "배터리",
            ResourceType.Fuel => "연료",
            _ => type.ToString()
        };

        private static ExplorationLocationData.RewardEntry RollReward(
            ExplorationLocationData.RewardEntry[] rewardTable)
        {
            float totalWeight = 0f;
            for (int i = 0; i < rewardTable.Length; i++)
                totalWeight += Mathf.Max(0f, rewardTable[i].selectionWeight);

            if (totalWeight <= 0f)
                return rewardTable[Random.Range(0, rewardTable.Length)];

            float roll = Random.value * totalWeight;
            for (int i = 0; i < rewardTable.Length; i++)
            {
                float weight = Mathf.Max(0f, rewardTable[i].selectionWeight);
                if (weight <= 0f) continue;
                if (roll < weight) return rewardTable[i];
                roll -= weight;
            }

            return rewardTable[rewardTable.Length - 1];
        }

        private static bool TryRollDifferentReward(
            ExplorationLocationData.RewardEntry[] rewardTable,
            ResourceType excludedType,
            out ExplorationLocationData.RewardEntry reward)
        {
            float totalWeight = 0f;
            for (int i = 0; i < rewardTable.Length; i++)
            {
                if (rewardTable[i].type == excludedType) continue;
                totalWeight += Mathf.Max(0f, rewardTable[i].selectionWeight);
            }

            if (totalWeight <= 0f)
            {
                reward = default;
                return false;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < rewardTable.Length; i++)
            {
                if (rewardTable[i].type == excludedType) continue;
                float weight = Mathf.Max(0f, rewardTable[i].selectionWeight);
                if (weight <= 0f) continue;
                if (roll < weight)
                {
                    reward = rewardTable[i];
                    return true;
                }
                roll -= weight;
            }

            reward = default;
            return false;
        }

        private static int RollRewardAmount(
            ExplorationLocationData.RewardEntry reward,
            bool takeGear,
            bool critical,
            out bool equipmentBonusGranted)
        {
            int amount = RollAmount(reward, takeGear);
            equipmentBonusGranted =
                takeGear &&
                (reward.type == ResourceType.Food ||
                 reward.type == ResourceType.Water) &&
                Random.value < EquippedFoodWaterBonusChance;
            if (equipmentBonusGranted) amount += 1;
            if (critical) amount = Mathf.Max(1, Mathf.FloorToInt(amount * 0.5f));
            return amount;
        }

        private static int RollAmount(
            ExplorationLocationData.RewardEntry reward,
            bool takeGear)
        {
            ExplorationLocationData.AmountChance[] chances =
                takeGear && reward.equippedAmountChances != null &&
                reward.equippedAmountChances.Length > 0
                    ? reward.equippedAmountChances
                    : reward.amountChances;
            if (chances == null || chances.Length == 0)
            {
                int min = Mathf.Max(0, reward.minAmount);
                int max = Mathf.Max(min, reward.maxAmount);
                return Random.Range(min, max + 1);
            }

            float totalWeight = 0f;
            for (int i = 0; i < chances.Length; i++)
                totalWeight += Mathf.Max(0f, chances[i].weight);
            if (totalWeight <= 0f)
                return Mathf.Max(0, chances[0].amount);

            float roll = Random.value * totalWeight;
            for (int i = 0; i < chances.Length; i++)
            {
                float weight = Mathf.Max(0f, chances[i].weight);
                if (weight <= 0f) continue;
                if (roll < weight) return Mathf.Max(0, chances[i].amount);
                roll -= weight;
            }

            return Mathf.Max(0, chances[chances.Length - 1].amount);
        }

        private static float BaseDangerForStars(int dangerStars) =>
            0.15f + (Mathf.Clamp(dangerStars, 1, 4) - 1) * 0.05f;
    }
}
