using YesterdayMap.Interaction;
using YesterdayMap.UI;

namespace YesterdayMap.Shelter
{
    public abstract class ShelterObject : InteractableObject
    {
        [field: UnityEngine.SerializeField] protected UIManager Ui { get; private set; }
        public bool IsBroken { get; private set; }

        public void ConfigureBase(UIManager ui, string prompt)
        {
            Ui = ui;
            SetPrompt(prompt);
        }

        public virtual void Break() => IsBroken = true;
        public virtual void Repair() => IsBroken = false;
        public void RestoreBrokenState(bool isBroken) => IsBroken = isBroken;

        protected bool RejectIfBroken()
        {
            if (!IsBroken) return false;
            Ui.ShowMessage($"{name} 시설이 고장 났습니다. 작업대에서 수리하세요.");
            return true;
        }
    }
}
