using UnityEngine;
using YesterdayMap.Audio;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Resources;

namespace YesterdayMap.Shelter
{
    public sealed class GeneratorObject : ShelterObject
    {
        public const float FuelChargePerItem = 25f;

        [SerializeField] private GeneratorPowerManager powerManager;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private PlayerCarryController carryController;

        public override string InteractionPrompt
        {
            get
            {
                ResolveReferences();
                if (powerManager == null)
                {
                    return
                        "[E] 발전기 가동\n발전기 연료 잔량: 확인 불가";
                }

                string action =
                    powerManager.IsRunning ? "정지" : "가동";
                return
                    $"[E] 발전기 {action}\n발전기 연료 잔량: " +
                    $"{powerManager.PowerPercent:0}%";
            }
        }

        public override string AlternateInteractionPrompt
        {
            get
            {
                ResolveReferences();
                if (powerManager == null)
                {
                    return "[F] 연료 넣기 (확인 불가)";
                }

                if (carryController != null &&
                    carryController.HasFuelCan)
                {
                    return powerManager.PowerPercent <=
                           100f - FuelChargePerItem
                        ? $"[F] 들고 있는 연료통 넣기 (+{FuelChargePerItem:0}%)"
                        : "[F] 연료통 넣기 (잔량 75% 이하에서 가능)";
                }

                int fuelCount = resources != null
                    ? resources.GetAmount(ResourceType.Fuel)
                    : 0;
                return fuelCount > 0
                    ? "[F] 연료 넣기 (먼저 선반에서 E로 들기)"
                    : "[F] 연료 넣기 (남은 연료통 없음)";
            }
        }

        public override bool CanAlternateInteract => true;

        public void Configure(
            ResourceManager manager,
            DayCycleManager cycle)
        {
            resources = manager;
        }

        public override void Interact()
        {
            ResolveReferences();
            if (powerManager == null)
            {
                return;
            }

            // E는 연료통 보유 여부와 관계없이 발전기 가동/정지만 담당합니다.
            powerManager.SetRunning(!powerManager.IsRunning);
            DayOneTutorialController.Instance?.NotifyGeneratorRunningChanged();
        }

        public override void AlternateInteract()
        {
            ResolveReferences();
            if (powerManager == null)
            {
                Ui?.ShowMessage("발전기 정보를 찾을 수 없습니다.");
                return;
            }

            if (carryController == null ||
                !carryController.HasFuelCan)
            {
                Ui?.ShowMessage(
                    "먼저 창고 선반의 빨간 연료통을 E키로 드세요.");
                return;
            }

            if (powerManager.PowerPercent >
                100f - FuelChargePerItem)
            {
                Ui?.ShowMessage(
                    "발전기 잔량이 75% 이하일 때 연료통을 넣을 수 있습니다.");
                return;
            }

            if (!carryController.TryConsumeFuelCan())
            {
                Ui?.ShowMessage("들고 있는 연료통을 넣지 못했습니다.");
                return;
            }

            powerManager.AddPower(FuelChargePerItem);
            GameSfxPlayer.Play(GameSfxCue.UseItem);
            DayOneTutorialController.Instance?.NotifyGeneratorFueled();

            string restartNotice = powerManager.IsRunning
                ? string.Empty
                : " E키를 눌러 발전기를 가동하세요.";
            Ui?.ShowMessage(
                $"연료통을 넣어 발전기를 {FuelChargePerItem:0}% 충전했습니다. " +
                $"(현재 {powerManager.PowerPercent:0}%){restartNotice}");
        }

        private void ResolveReferences()
        {
            if (powerManager == null)
            {
                powerManager =
                    FindFirstObjectByType<GeneratorPowerManager>();
            }

            if (resources == null)
            {
                resources =
                    FindFirstObjectByType<ResourceManager>();
            }

            if (carryController == null)
            {
                carryController =
                    FindFirstObjectByType<PlayerCarryController>();
            }
        }
    }
}
