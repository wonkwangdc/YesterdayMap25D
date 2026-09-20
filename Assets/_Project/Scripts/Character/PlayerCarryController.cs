using System;
using UnityEngine;

namespace YesterdayMap.Character
{
    /// <summary>
    /// Tracks whether the player is carrying one fuel can.
    /// The can is represented by the HUD instead of a 3D object following the character.
    /// </summary>
    public sealed class PlayerCarryController : MonoBehaviour
    {
        [SerializeField] private bool hasFuelCan;

        public event Action<bool> FuelCanCarryChanged;

        public bool HasFuelCan => hasFuelCan;

        public bool TryCarryFuelCan()
        {
            if (hasFuelCan)
            {
                return false;
            }

            SetFuelCanState(true);
            return true;
        }

        public bool TryConsumeFuelCan()
        {
            if (!hasFuelCan)
            {
                return false;
            }

            SetFuelCanState(false);
            return true;
        }

        public void RestoreFuelCanState(bool isCarried)
        {
            SetFuelCanState(isCarried);
        }

        private void SetFuelCanState(bool isCarried)
        {
            if (hasFuelCan == isCarried)
            {
                return;
            }

            hasFuelCan = isCarried;
            FuelCanCarryChanged?.Invoke(hasFuelCan);
        }
    }
}

