using UnityEngine;
using YesterdayMap.Core;
using YesterdayMap.Resources;
using YesterdayMap.UI;

namespace YesterdayMap.Shelter
{
    public enum ShelterRepairTarget
    {
        WaterPurifier,
        Generator,
        Barricade
    }

    // Opens the facility repair view and performs the selected repair.
    public sealed class WorkbenchObject : ShelterObject
    {
        [Header("Facilities")]
        [SerializeField] private ResourceManager resources;
        [SerializeField] private WaterPurifierObject purifier;
        [SerializeField] private GeneratorObject generator;
        [SerializeField] private BarricadeObject barricade;
        [SerializeField] private WorkbenchRepairUI repairUI;

        [Header("Parts Cost")]
        [SerializeField, Min(1)] private int purifierRepairCost = 2;
        [SerializeField, Min(1)] private int generatorRepairCost = 3;
        [SerializeField, Min(1)] private int barricadeRepairCost = 4;

        public void Configure(
            ResourceManager manager,
            BarricadeObject wall,
            DayCycleManager cycle,
            GeneratorObject power,
            WaterPurifierObject water)
        {
            resources = manager;
            barricade = wall;
            generator = power;
            purifier = water;
        }

        public override void Interact()
        {
            if (repairUI == null)
            {
                repairUI = FindFirstObjectByType<WorkbenchRepairUI>(
                    FindObjectsInactive.Include);
            }

            if (repairUI == null)
            {
                Ui?.ShowMessage("수리 선택 화면을 찾을 수 없습니다.");
                return;
            }

            repairUI.Open(this);
        }

        public bool TryRepair(ShelterRepairTarget target, out string message)
        {
            ShelterObject facility = GetFacility(target);
            string label = GetRepairLabel(target);
            int cost = GetRepairCost(target);

            if (facility == null)
            {
                message = $"{label} 연결을 확인해 주세요.";
                return false;
            }

            if (!facility.IsBroken)
            {
                message = $"{label}은(는) 정상 상태입니다.";
                return false;
            }

            if (resources == null || !resources.TrySpend(ResourceType.Parts, cost))
            {
                message = $"{label} 수리에는 부품 {cost}개가 필요합니다.";
                return false;
            }

            facility.Repair();
            message = $"{label} 수리를 완료했습니다. 부품 -{cost}";
            Ui?.ShowMessage(message);
            return true;
        }

        public ShelterObject GetFacility(ShelterRepairTarget target)
        {
            return target switch
            {
                ShelterRepairTarget.WaterPurifier => purifier,
                ShelterRepairTarget.Generator => generator,
                ShelterRepairTarget.Barricade => barricade,
                _ => null
            };
        }

        public int GetRepairCost(ShelterRepairTarget target)
        {
            return target switch
            {
                ShelterRepairTarget.WaterPurifier => purifierRepairCost,
                ShelterRepairTarget.Generator => generatorRepairCost,
                ShelterRepairTarget.Barricade => barricadeRepairCost,
                _ => 0
            };
        }

        public string GetRepairLabel(ShelterRepairTarget target)
        {
            return target switch
            {
                ShelterRepairTarget.WaterPurifier => "정수기",
                ShelterRepairTarget.Generator => "발전기",
                ShelterRepairTarget.Barricade => "방어벽",
                _ => "시설"
            };
        }

        public bool HasPartsFor(ShelterRepairTarget target)
        {
            return resources != null &&
                   resources.Has(ResourceType.Parts, GetRepairCost(target));
        }
    }
}
