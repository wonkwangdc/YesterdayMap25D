using UnityEngine;
using YesterdayMap.Core;
using YesterdayMap.Shelter;
using YesterdayMap.UI;

namespace YesterdayMap.Events
{
    public sealed class EventManager : MonoBehaviour
    {
        [SerializeField] private UIManager ui;
        [SerializeField] private DayCycleManager dayCycle;

        public void Configure(DayCycleManager cycle, BarricadeObject wall, GeneratorObject power,
            WaterPurifierObject water, YesterdayMap.Core.GameManager manager, UIManager uiManager)
        {
            dayCycle = cycle;
            ui = uiManager;
        }

        public void ResolveNightEvent()
        {
            if (dayCycle != null && dayCycle.CurrentDay == 1) return;
            ui.ShowMessage("큰 사건 없이 밤이 지나갔습니다.");
        }
    }
}
