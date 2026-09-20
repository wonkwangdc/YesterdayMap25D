using UnityEngine;
using YesterdayMap.Audio;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Interaction;
using YesterdayMap.Resources;
using YesterdayMap.UI;

namespace YesterdayMap.Shelter
{
    /// <summary>
    /// Makes one of the existing red storage fuel visuals pickable with E.
    /// Picking it removes one shared fuel resource and switches on the HUD carry indicator.
    /// </summary>
    public sealed class FuelCanObject : InteractableObject
    {
        [SerializeField] private ResourceManager resources;
        [SerializeField] private PlayerCarryController carrier;
        [SerializeField] private ShelterResourceDisplay display;
        [SerializeField] private UIManager ui;

        public override string InteractionPrompt => "[E] 연료통 들기";
        public override bool RequiresCenterAim => true;

        public override bool CanInteract
        {
            get
            {
                ResolveReferences();
                return base.CanInteract &&
                       resources != null &&
                       resources.Has(ResourceType.Fuel, 1) &&
                       carrier != null &&
                       !carrier.HasFuelCan;
            }
        }

        public void Configure(
            ResourceManager manager,
            ShelterResourceDisplay owner)
        {
            resources = manager;
            display = owner;
            ResolveReferences();
        }

        public override void Interact()
        {
            ResolveReferences();
            if (resources == null || carrier == null)
            {
                ui?.ShowMessage("연료통 정보를 찾을 수 없습니다.");
                return;
            }

            if (carrier.HasFuelCan)
            {
                ui?.ShowMessage("이미 연료통을 하나 들고 있습니다.");
                return;
            }

            if (!resources.Has(ResourceType.Fuel, 1))
            {
                ui?.ShowMessage("선반에 남아 있는 연료통이 없습니다.");
                return;
            }

            if (!carrier.TryCarryFuelCan())
            {
                ui?.ShowMessage("연료통을 들지 못했습니다.");
                return;
            }

            // 클릭한 빨간 연료통이 실제로 사라지도록 목록의 마지막으로 보낸 뒤
            // ResourceManager 수량을 줄입니다. 수량 변경 이벤트가 즉시 표시를 갱신합니다.
            bool detachedFromDisplay = display != null &&
                                       display.DetachFuelVisualForPickup(gameObject);
            if (!detachedFromDisplay)
            {
                gameObject.SetActive(false);
            }
            if (!resources.TrySpend(ResourceType.Fuel, 1))
            {
                carrier.RestoreFuelCanState(false);
                if (detachedFromDisplay)
                {
                    display.RestoreDetachedFuelVisual(gameObject);
                }
                else
                {
                    gameObject.SetActive(true);
                }
                ui?.ShowMessage("연료통을 들지 못했습니다.");
                return;
            }

            // 자원 변경 이벤트와 관계없이 집은 연료통은 즉시 화면과 상호작용에서 제거합니다.
            // 표시 목록에서는 이미 마지막 슬롯으로 이동했으므로 이후 Refresh에서도 다시 켜지지 않습니다.
            if (display != null)
            {
                display.FinalizeFuelVisualRemoval(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }

            GameSfxPlayer.Play(GameSfxCue.Pickup);
            DayOneTutorialController.Instance?.NotifyFuelCanPickedUp();
            ui?.ShowMessage(
                "연료통을 들었습니다. 발전기 앞에서 F키를 누르세요.");
        }

        private void ResolveReferences()
        {
            if (resources == null)
            {
                resources = FindFirstObjectByType<ResourceManager>();
            }

            if (carrier == null)
            {
                carrier = FindFirstObjectByType<PlayerCarryController>();
            }

            if (display == null)
            {
                display = GetComponentInParent<ShelterResourceDisplay>();
            }

            if (ui == null)
            {
                ui = FindFirstObjectByType<UIManager>();
            }
        }
    }
}

