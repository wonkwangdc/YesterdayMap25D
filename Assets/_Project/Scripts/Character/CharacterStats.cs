using System;
using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.Core;

namespace YesterdayMap.Character
{
    // Owns independent player stats and their morning survival judgments.
    public sealed class CharacterStats : MonoBehaviour
    {
        private const float MinValue = 0f;
        private const float MaxValue = 100f;

        [SerializeField, Range(0, 100)] private float health = 100f;
        [SerializeField, Range(0, 100)] private float hunger = 85f;
        [SerializeField, Range(0, 100)] private float thirst = 85f;
        [SerializeField, Range(0, 100)] private float morale = 75f;

        private readonly List<string> survivalRecords = new();
        private readonly List<string> morningDialogueLines = new();
        private int morningDialogueDay = -1;
        private int hungerWarningStage;
        private int thirstWarningStage;
        private int zeroSequence;
        private int hungerZeroSequence = int.MaxValue;
        private int thirstZeroSequence = int.MaxValue;
        private bool healthAwaitingMorning;
        private bool deathResolved;

        public event Action StatsChanged;
        public event Action<string> Died;
        public event Action<string> WarningRaised;

        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Morale => morale;
        public bool IsCriticalCondition { get; private set; }
        public bool RequiresFatalSleepConfirmation =>
            IsCriticalCondition && health <= MinValue && !deathResolved;
        public bool CanExplore => morale > MinValue && !deathResolved;
        public IReadOnlyList<string> SurvivalRecords => survivalRecords.AsReadOnly();
        public IReadOnlyList<string> MorningDialogueLines =>
            morningDialogueLines.AsReadOnly();
        public int MorningDialogueDay => morningDialogueDay;
        public string LatestSurvivalRecord => survivalRecords.Count == 0
            ? string.Empty
            : survivalRecords[survivalRecords.Count - 1];

        public void ModifyHealth(float amount, string damageReason = "Injury")
        {
            float previous = health;
            health = Clamp(health + amount);

            if (previous > MinValue && health <= MinValue)
            {
                healthAwaitingMorning = true;
                IsCriticalCondition = false;
                RaiseWarning("체력이 0이 되었습니다. 다음 날 아침 빈사 상태가 됩니다.");
            }
            else if (health > MinValue)
            {
                healthAwaitingMorning = false;
                IsCriticalCondition = false;
            }

            StatsChanged?.Invoke();
        }

        public void ModifyHunger(float amount)
        {
            float previous = hunger;
            hunger = Clamp(hunger + amount);
            UpdateNeedRecoveryAndZeroOrder(
                previous,
                hunger,
                ref hungerWarningStage,
                ref hungerZeroSequence);
            StatsChanged?.Invoke();
        }

        public void ModifyThirst(float amount)
        {
            float previous = thirst;
            thirst = Clamp(thirst + amount);
            UpdateNeedRecoveryAndZeroOrder(
                previous,
                thirst,
                ref thirstWarningStage,
                ref thirstZeroSequence);
            StatsChanged?.Invoke();
        }

        public void ModifyMorale(float amount)
        {
            float previous = morale;
            morale = Clamp(morale + amount);
            if (previous > MinValue && morale <= MinValue)
            {
                RaiseWarning("정신력이 0이 되어 탐사를 진행할 수 없습니다.");
            }
            else if (previous <= MinValue && morale > MinValue)
            {
                RaiseWarning("정신력을 회복해 다시 탐사할 수 있습니다.");
            }

            StatsChanged?.Invoke();
        }

        public void ApplySleepNeeds()
        {
            // Sleeping changes each need independently; depleted needs never damage health.
            ModifyHunger(-20f);
            ModifyThirst(-30f);
            ModifyMorale(-10f);
        }

        public void ProcessMorning(int day)
        {
            morningDialogueLines.Clear();
            morningDialogueDay = day;
            if (deathResolved)
            {
                return;
            }

            if (health <= MinValue && healthAwaitingMorning)
            {
                healthAwaitingMorning = false;
                IsCriticalCondition = true;
                RaiseWarning($"{day}일차 아침: 체력 0으로 빈사 상태입니다. 회복하지 않고 다시 잠들면 사망합니다.");
            }

            bool starvationFatal = AdvanceNeedWarning(
                day,
                hunger,
                ref hungerWarningStage,
                "굶주림",
                "배고프다… 오늘은 뭐라도 좀 먹어야겠는데.",
                "힘이 하나도 없네… 이대로 하루 더 버티는 건 무리야.");
            bool dehydrationFatal = AdvanceNeedWarning(
                day,
                thirst,
                ref thirstWarningStage,
                "탈수",
                "목이 너무 마르네… 물부터 좀 마셔야겠다.",
                "입안이 다 말랐어… 진짜 더는 못 버티겠다.");

            if (!starvationFatal && !dehydrationFatal)
            {
                return;
            }

            if (starvationFatal && dehydrationFatal)
            {
                ResolveDeath(hungerZeroSequence <= thirstZeroSequence
                    ? "Starvation"
                    : "Dehydration");
            }
            else
            {
                ResolveDeath(starvationFatal ? "Starvation" : "Dehydration");
            }
        }

        public void ResolveFatalSleep()
        {
            if (RequiresFatalSleepConfirmation)
            {
                ResolveDeath("FatalInjury");
            }
        }

        public bool PrepareNeedDeathEveForTesting(string deathReason)
        {
            bool starvation = deathReason == "Starvation";
            bool dehydration = deathReason == "Dehydration";
            if (!starvation && !dehydration)
            {
                return false;
            }

            health = MaxValue;
            morale = MaxValue;
            hunger = starvation ? MinValue : MaxValue;
            thirst = dehydration ? MinValue : MaxValue;
            hungerWarningStage = starvation ? 2 : 0;
            thirstWarningStage = dehydration ? 2 : 0;
            zeroSequence++;
            hungerZeroSequence = starvation ? zeroSequence : int.MaxValue;
            thirstZeroSequence = dehydration ? zeroSequence : int.MaxValue;
            healthAwaitingMorning = false;
            IsCriticalCondition = false;
            deathResolved = false;
            morningDialogueLines.Clear();
            morningDialogueDay = -1;
            StatsChanged?.Invoke();
            return true;
        }

        public CharacterStatsSaveData CaptureState()
        {
            return new CharacterStatsSaveData
            {
                health = health,
                hunger = hunger,
                thirst = thirst,
                morale = morale,
                hungerWarningStage = hungerWarningStage,
                thirstWarningStage = thirstWarningStage,
                zeroSequence = zeroSequence,
                hungerZeroSequence = hungerZeroSequence,
                thirstZeroSequence = thirstZeroSequence,
                healthAwaitingMorning = healthAwaitingMorning,
                isCriticalCondition = IsCriticalCondition,
                deathResolved = deathResolved,
                survivalRecords = survivalRecords.ToArray()
            };
        }

        public void RestoreState(CharacterStatsSaveData data)
        {
            if (data == null) return;

            health = Clamp(data.health);
            hunger = Clamp(data.hunger);
            thirst = Clamp(data.thirst);
            morale = Clamp(data.morale);
            hungerWarningStage = Mathf.Max(0, data.hungerWarningStage);
            thirstWarningStage = Mathf.Max(0, data.thirstWarningStage);
            zeroSequence = Mathf.Max(0, data.zeroSequence);
            hungerZeroSequence = data.hungerZeroSequence;
            thirstZeroSequence = data.thirstZeroSequence;
            healthAwaitingMorning = data.healthAwaitingMorning;
            IsCriticalCondition = data.isCriticalCondition;
            deathResolved = data.deathResolved;

            survivalRecords.Clear();
            morningDialogueLines.Clear();
            morningDialogueDay = -1;
            if (data.survivalRecords != null)
                survivalRecords.AddRange(data.survivalRecords);

            StatsChanged?.Invoke();
        }

        private bool AdvanceNeedWarning(
            int day,
            float value,
            ref int stage,
            string label,
            string firstWarningDialogue,
            string secondWarningDialogue)
        {
            if (value > MinValue)
            {
                stage = 0;
                return false;
            }

            stage++;
            if (stage == 1)
            {
                RaiseWarning($"{day}일차 아침: {label} 1차 경고. 다음 아침 전까지 회복해야 합니다.");
                morningDialogueLines.Add(firstWarningDialogue);
                return false;
            }

            if (stage == 2)
            {
                RaiseWarning($"{day}일차 아침: {label} 2차 경고. 이번이 마지막 회복 기회입니다.");
                morningDialogueLines.Add(secondWarningDialogue);
                return false;
            }

            return true;
        }

        private void UpdateNeedRecoveryAndZeroOrder(
            float previous,
            float current,
            ref int warningStage,
            ref int reachedZeroSequence)
        {
            if (previous > MinValue && current <= MinValue)
            {
                reachedZeroSequence = ++zeroSequence;
            }
            else if (previous <= MinValue && current > MinValue)
            {
                warningStage = 0;
                reachedZeroSequence = int.MaxValue;
            }
        }

        private void RaiseWarning(string message)
        {
            survivalRecords.Add(message);
            WarningRaised?.Invoke(message);
        }

        private void ResolveDeath(string reason)
        {
            if (deathResolved)
            {
                return;
            }

            deathResolved = true;
            Died?.Invoke(reason);
        }

        private static float Clamp(float value) =>
            Mathf.Clamp(value, MinValue, MaxValue);
    }
}
