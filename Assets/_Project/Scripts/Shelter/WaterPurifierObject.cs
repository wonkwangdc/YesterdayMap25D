using YesterdayMap.Core;
using YesterdayMap.Resources;

namespace YesterdayMap.Shelter
{
    public sealed class WaterPurifierObject : ShelterObject
    {
        [UnityEngine.SerializeField, UnityEngine.Min(0.5f)] private float workDuration = 2f;
        [UnityEngine.SerializeField] private ResourceManager resources;

        public void Configure(ResourceManager manager, DayCycleManager cycle)
        {
            resources = manager;
        }

        public override void Interact()
        {
            if (RejectIfBroken()) return;
            if (!resources.Has(ResourceType.Battery, 1))
            {
                Ui.ShowMessage("정수기에는 배터리 1개가 필요합니다.");
                return;
            }

            resources.TrySpend(ResourceType.Battery, 1);
            Ui.StartWork("식수 정수 중...", workDuration, () =>
            {
                resources.Add(ResourceType.Water, 4);
                Ui.ShowMessage("정수를 마쳤습니다. 식수 2일분을 확보했습니다.");
            });
        }
    }
}
