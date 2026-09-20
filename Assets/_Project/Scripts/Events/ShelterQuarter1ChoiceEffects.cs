using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.Character;
using YesterdayMap.Resources;

namespace YesterdayMap.Events
{
    public readonly struct Quarter1ChoiceEffectResult
    {
        public string NarrativeText { get; }
        public string SystemResultText { get; }

        public Quarter1ChoiceEffectResult(string narrativeText, string systemResultText)
        {
            NarrativeText = narrativeText ?? string.Empty;
            SystemResultText = systemResultText ?? string.Empty;
        }

        public string BuildDiaryText()
        {
            if (string.IsNullOrWhiteSpace(SystemResultText))
            {
                return NarrativeText;
            }

            return $"{NarrativeText}\n\n{SystemResultText}";
        }
    }

    /// <summary>
    /// CSV에 정의된 1분기 선택 결과를 실제 캐릭터 상태와 공용 자원에 적용한다.
    /// 계열 점수는 BranchOneFlowController가 담당한다.
    /// </summary>
    public static class ShelterQuarter1ChoiceEffects
    {
        private const float SmallStatChange = 5f;
        private const float NormalStatRecovery = 10f;
        private const float LargeStatRecovery = 15f;
        private const float VeryLargeStatRecovery = 20f;
        private const float NeedRecovery = 20f;
        private const float NormalHealthDamage = 10f;
        private const int ConsumableUnits = 2;

        public static bool CanApply(
            BranchEventDefinition definition,
            BranchEventChoice choice,
            ResourceManager resources,
            out string failureReason)
        {
            failureReason = string.Empty;
            if (definition == null || choice == null)
            {
                return true;
            }

            bool first = IsFirstChoice(definition, choice);
            switch (definition.EventNumber)
            {
                case 4 when first:
                    return RequireResource(resources, ResourceType.Food, ConsumableUnits, "통조림이 없습니다.", out failureReason);
                case 7 when first:
                    return RequireResource(resources, ResourceType.Parts, 1, "만능 수리키트로 사용할 부품이 없습니다.", out failureReason);
                case 9 when first:
                    return RequireResource(resources, ResourceType.Water, ConsumableUnits, "버릴 수 있는 물이 없습니다.", out failureReason);
                case 10:
                    return RequireResource(resources, ResourceType.Food, ConsumableUnits, "확인할 통조림이 없습니다.", out failureReason);
                case 11:
                    return RequireResource(resources, ResourceType.Water, ConsumableUnits, "옮기거나 마실 물이 없습니다.", out failureReason);
                case 12 when first:
                    return RequireResource(resources, ResourceType.Medicine, 1, "사용할 구급상자가 없습니다.", out failureReason);
                case 22 when first:
                    if (resources == null ||
                        (!resources.Has(ResourceType.Food, ConsumableUnits) &&
                         !resources.Has(ResourceType.Water, ConsumableUnits)))
                    {
                        failureReason = "밖에 내놓을 통조림이나 물이 없습니다.";
                        return false;
                    }
                    break;
                case 24 when first:
                    return RequireResource(resources, ResourceType.Water, ConsumableUnits, "물병을 채울 물이 없습니다.", out failureReason);
            }

            return true;
        }

        public static Quarter1ChoiceEffectResult Apply(
            BranchEventDefinition definition,
            BranchEventChoice choice,
            CharacterStats stats,
            ResourceManager resources)
        {
            if (definition == null || choice == null)
            {
                return new Quarter1ChoiceEffectResult(string.Empty, string.Empty);
            }

            bool first = IsFirstChoice(definition, choice);
            string narrativeText = choice.ResultText;

            switch (definition.EventNumber)
            {
                case 1:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 2:
                    if (first) ModifyMorale(stats, NormalStatRecovery);
                    break;
                case 3:
                    ModifyMorale(stats, first ? SmallStatChange : -SmallStatChange);
                    break;
                case 4:
                    if (first)
                    {
                        resources?.TrySpend(ResourceType.Food, ConsumableUnits);
                        ModifyMorale(stats, LargeStatRecovery);
                        stats?.ModifyHunger(NeedRecovery);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 5:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                    }
                    else
                    {
                        SpendRandomOwnedItem(resources);
                    }
                    break;
                case 6:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 7:
                    if (first)
                    {
                        resources?.TrySpend(ResourceType.Parts, 1);
                        ModifyMorale(stats, VeryLargeStatRecovery);
                    }
                    else
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 8:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        bool repaired = Random.value < 0.5f;
                        if (repaired)
                        {
                            ModifyMorale(stats, NormalStatRecovery);
                            narrativeText =
                                "깜박이던 비상등을 직접 분해해 고쳤다.\n" +
                                "안정된 불빛이 다시 벙커 안을 채우자, 사소한 고장 하나를 해결했을 뿐인데도 마음이 한결 놓였다.";
                        }
                        else
                        {
                            ModifyMorale(stats, -SmallStatChange);
                            narrativeText =
                                "비상등을 뜯어보았지만 끝내 고장 원인을 찾지 못했다.\n" +
                                "시간과 힘만 쓰고 불빛은 더 불안하게 흔들려, 괜히 건드린 것은 아닌지 후회가 남았다.";
                        }
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 9:
                    if (first)
                    {
                        resources?.TrySpend(ResourceType.Water, ConsumableUnits);
                    }
                    else
                    {
                        ModifyHealth(stats, -SmallStatChange);
                    }
                    break;
                case 10:
                    resources?.TrySpend(ResourceType.Food, ConsumableUnits);
                    if (!first)
                    {
                        stats?.ModifyHunger(NeedRecovery);
                        bool causedDamage = Random.value < 0.5f;
                        if (causedDamage)
                        {
                            ModifyHealth(stats, -NormalHealthDamage);
                            narrativeText =
                                "통조림을 먹은 뒤 얼마 지나지 않아 심한 복통이 시작되었다.\n" +
                                "아깝다는 생각에 위험한 음식을 억지로 먹은 대가를 제대로 치른 셈이다.";
                        }
                        else
                        {
                            narrativeText =
                                "통조림의 냄새를 확인한 뒤 조심스럽게 먹었다.\n" +
                                "맛도 냄새도 크게 이상하지 않았고, 배를 채울 수 있었으니 이번에는 운이 좋았던 것 같다.";
                        }
                    }
                    break;
                case 11:
                    if (first)
                    {
                        resources?.TrySpend(ResourceType.Water, ConsumableUnits);
                        stats?.ModifyThirst(NeedRecovery);
                    }
                    else
                    {
                        bool preserved = Random.value < 0.5f;
                        if (preserved)
                        {
                            narrativeText =
                                "금이 간 물병의 물을 다른 용기로 천천히 옮겼다.\n" +
                                "한 방울도 흘리지 않고 보관하는 데 성공해, 귀한 물을 지켜냈다는 안도감이 들었다.";
                        }
                        else
                        {
                            resources?.TrySpend(ResourceType.Water, ConsumableUnits);
                            narrativeText =
                                "물을 옮기던 도중 병의 금이 갑자기 크게 벌어졌다.\n" +
                                "손쓸 틈도 없이 물이 바닥으로 쏟아졌고, 눈앞에서 사라지는 물을 바라볼 수밖에 없었다.";
                        }
                    }
                    break;
                case 12:
                    if (first)
                    {
                        resources?.TrySpend(ResourceType.Medicine, 1);
                    }
                    else
                    {
                        ModifyHealth(stats, -NormalHealthDamage);
                    }
                    break;
                case 13:
                    ModifyMorale(stats, first ? SmallStatChange : -SmallStatChange);
                    break;
                case 14:
                    if (first)
                    {
                        narrativeText = ResolveAbandonedBag(resources);
                    }
                    break;
                case 15:
                    if (first) ModifyMorale(stats, SmallStatChange);
                    break;
                case 16:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    break;
                case 17:
                    ModifyMorale(stats, first ? SmallStatChange : -SmallStatChange);
                    break;
                case 18:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 19:
                    if (first)
                    {
                        ModifyHealth(stats, -SmallStatChange);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 20:
                    ModifyMorale(stats, -SmallStatChange);
                    break;
                case 21:
                    if (first) ModifyMorale(stats, SmallStatChange);
                    break;
                case 22:
                    if (first)
                    {
                        SpendFoodOrWater(resources);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 23:
                    ModifyMorale(stats, first ? SmallStatChange : -SmallStatChange);
                    break;
                case 24:
                    if (first)
                    {
                        resources?.TrySpend(ResourceType.Water, ConsumableUnits);
                        ModifyMorale(stats, SmallStatChange);
                    }
                    else
                    {
                        ModifyMorale(stats, -SmallStatChange);
                    }
                    break;
                case 25:
                    ModifyHealth(stats, -SmallStatChange);
                    if (first) ModifyMorale(stats, SmallStatChange);
                    break;
                case 26:
                    ModifyMorale(stats, first ? SmallStatChange : -SmallStatChange);
                    break;
                case 1007:
                    if (first) ModifyMorale(stats, SmallStatChange);
                    break;
            }

            return new Quarter1ChoiceEffectResult(
                narrativeText,
                GetSystemResultText(definition.EventNumber, first));
        }

        private static string ResolveAbandonedBag(ResourceManager resources)
        {
            if (resources != null && Random.value < 0.5f)
            {
                ResourceType type = Random.value < 0.5f
                    ? ResourceType.Food
                    : ResourceType.Water;
                resources.Add(type, ConsumableUnits);
                return
                    "주변을 충분히 살핀 뒤 출입구 앞에 놓인 가방을 벙커 안으로 가져왔다.\n" +
                    "가방 안에는 아직 사용할 수 있는 물자가 남아 있었고, 누가 두고 갔는지는 끝내 알 수 없었다.";
            }

            return
                "위험을 감수하고 가방을 가져왔지만 안에는 쓸 만한 것이 하나도 없었다.\n" +
                "괜한 기대를 했다는 허탈함과, 누군가 일부러 빈 가방을 둔 것은 아닐지 모른다는 불안만 남았다.";
        }

        private static void SpendFoodOrWater(ResourceManager resources)
        {
            if (resources == null)
            {
                return;
            }

            if (resources.Has(ResourceType.Food, ConsumableUnits))
            {
                resources.TrySpend(ResourceType.Food, ConsumableUnits);
            }
            else
            {
                resources.TrySpend(ResourceType.Water, ConsumableUnits);
            }
        }

        private static void SpendRandomOwnedItem(ResourceManager resources)
        {
            if (resources == null)
            {
                return;
            }

            List<ResourceType> candidates = new();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                int required = type is ResourceType.Food or ResourceType.Water
                    ? ConsumableUnits
                    : 1;
                if (resources.Has(type, required))
                {
                    candidates.Add(type);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            ResourceType selected = candidates[Random.Range(0, candidates.Count)];
            int amount = selected is ResourceType.Food or ResourceType.Water
                ? ConsumableUnits
                : 1;
            resources.TrySpend(selected, amount);
        }

        private static bool RequireResource(
            ResourceManager resources,
            ResourceType type,
            int amount,
            string message,
            out string failureReason)
        {
            if (resources != null && resources.Has(type, amount))
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason = message;
            return false;
        }

        private static bool IsFirstChoice(
            BranchEventDefinition definition,
            BranchEventChoice choice)
        {
            return definition.Choices.Count > 0 &&
                   ReferenceEquals(definition.Choices[0], choice);
        }

        private static void ModifyHealth(CharacterStats stats, float amount)
        {
            stats?.ModifyHealth(amount, "1분기 일반 이벤트");
        }

        private static void ModifyMorale(CharacterStats stats, float amount)
        {
            stats?.ModifyMorale(amount);
        }

        private static string GetSystemResultText(int eventNumber, bool first)
        {
            return (eventNumber, first) switch
            {
                (1, true) => "• 체력 소폭 감소\n• 정신력 소폭 상승\n• 벙커 내부 환경이 개선되었다",
                (1, false) => "• 체력 변화 없음\n• 정신력 소폭 감소",
                (2, true) => "• 정신력 회복\n• 시간 소모",
                (2, false) => "• 스탯 및 아이템 변화 없음",
                (3, true) => "• 정신력 소폭 회복\n• 시간 소모",
                (3, false) => "• 정신력 소폭 감소",
                (4, true) => "• 통조림 1개 소모\n• 정신력 크게 회복\n• 배고픔 회복",
                (4, false) => "• 아이템 소모 없음\n• 정신력 소폭 감소",
                (5, true) => "• 체력 소폭 감소\n• 물자 손실 방지\n• 벙커 내부 환경이 개선되었다",
                (5, false) => "• 체력 변화 없음\n• 보유 아이템 1개 손실",
                (6, true) => "• 체력 소폭 감소\n• 정신력 소폭 회복\n• 벙커 내부 환경이 개선되었다",
                (6, false) => "• 체력 변화 없음\n• 정신력 소폭 감소\n• 벙커 내부 환경이 악화되었다",
                (7, true) => "• 만능 수리키트 1개 소모\n• 정신력 대폭 상승\n• 벙커 내부 환경이 개선되었다",
                (7, false) => "• 체력 소폭 감소\n• 정신력 소폭 감소\n• 아이템 소모 없음",
                (8, true) => "• 체력 소폭 감소\n• 일정 확률로 수리 성공\n• 수리 성공 시 정신력 회복\n• 수리 성공 시 벙커 내부 환경이 개선되었다\n• 수리 실패 시 정신력 소폭 감소",
                (8, false) => "• 정신력 소폭 감소",
                (9, true) => "• 물 1개 소모\n• 체력 피해 방지\n• 벙커 내부 환경이 개선되었다",
                (9, false) => "• 물 소모 없음\n• 체력 소폭 감소",
                (10, true) => "• 통조림 1개 소모\n• 체력 피해 방지",
                (10, false) => "• 통조림 1개 소모\n• 배고픔 회복\n• 일정 확률로 체력 감소",
                (11, true) => "• 물 1개 소모\n• 갈증 회복\n• 물 손실 방지",
                (11, false) => "• 일정 확률로 물 1개 보존\n• 실패 시 물 1개 손실\n• 갈증은 회복되지 않음",
                (12, true) => "• 구급상자 1개 소모\n• 체력 피해 방지",
                (12, false) => "• 아이템 소모 없음\n• 체력 감소",
                (13, true) => "• 정신력 소폭 회복",
                (13, false) => "• 정신력 소폭 감소",
                (14, true) => "• 일정 확률로 통조림 또는 물 1개 획득\n• 일정 확률로 아무것도 획득하지 못함",
                (14, false) => "• 스탯 및 아이템 변화 없음",
                (15, true) => "• 정신력 소폭 상승",
                (15, false) => "• 스탯 및 아이템 변화 없음",
                (16, true) => "• 체력 소폭 감소\n• 정신력 소폭 회복",
                (16, false) => "• 스탯 및 아이템 변화 없음",
                (17, true) => "• 정신력 소폭 회복",
                (17, false) => "• 정신력 소폭 감소",
                (18, true) => "• 체력 소폭 감소\n• 정신력 소폭 상승",
                (18, false) => "• 정신력 소폭 감소",
                (19, true) => "• 체력 소폭 감소\n• 정신력 소폭 상승",
                (19, false) => "• 체력 변화 없음\n• 정신력 소폭 감소",
                (20, true) => "• 정신력 소폭 감소",
                (20, false) => "• 정신력 소폭 감소",
                (21, true) => "• 정신력 소폭 회복",
                (21, false) => "• 스탯 및 아이템 변화 없음",
                (22, true) => "• 통조림 또는 물 1개 소모\n• 정신력 소폭 회복",
                (22, false) => "• 아이템 소모 없음\n• 정신력 소폭 감소",
                (23, true) => "• 정신력 소폭 회복",
                (23, false) => "• 정신력 소폭 감소",
                (24, true) => "• 물 1개 소모\n• 정신력 소폭 회복",
                (24, false) => "• 아이템 소모 없음\n• 정신력 소폭 감소",
                (25, true) => "• 체력 소폭 감소\n• 정신력 소폭 상승",
                (25, false) => "• 체력 소폭 감소",
                (26, true) => "• 정신력 소폭 회복",
                (26, false) => "• 정신력 소폭 감소",
                (1007, true) => "• 정신력 소폭 회복",
                (1007, false) => "• 스탯 및 아이템 변화 없음",
                _ => string.Empty
            };
        }
    }
}
