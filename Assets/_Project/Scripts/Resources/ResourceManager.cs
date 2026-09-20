using System;
using UnityEngine;
using YesterdayMap.Core;

namespace YesterdayMap.Resources
{
    // 벙커가 공유하는 물자와 변경 알림만 담당한다.
    public sealed class ResourceManager : MonoBehaviour
    {
        [Header("초기 자원 (식량과 물은 반 단위 관리를 위해 2배 단위)")]
        [SerializeField] private int food = 1;
        [SerializeField] private int water = 1;
        [SerializeField] private int medicine = 1;
        [SerializeField] private int parts;
        [SerializeField] private int battery;
        [SerializeField] private int fuel = 1;

        public event Action ResourcesChanged;

        public int GetAmount(ResourceType type)
        {
            return type switch
            {
                ResourceType.Food => food,
                ResourceType.Water => water,
                ResourceType.Medicine => medicine,
                ResourceType.Parts => parts,
                ResourceType.Battery => battery,
                ResourceType.Fuel => fuel,
                _ => 0
            };
        }

        public bool Has(ResourceType type, int amount) => GetAmount(type) >= Mathf.Max(0, amount);

        public bool TrySpend(ResourceType type, int amount)
        {
            amount = Mathf.Max(0, amount);
            if (!Has(type, amount)) return false;
            SetAmount(type, GetAmount(type) - amount);
            return true;
        }

        public void Add(ResourceType type, int amount)
        {
            SetAmount(type, Mathf.Max(0, GetAmount(type) + amount));
        }

        public ResourceSaveData CaptureState()
        {
            int count = Enum.GetValues(typeof(ResourceType)).Length;
            int[] amounts = new int[count];
            for (int i = 0; i < count; i++)
                amounts[i] = GetAmount((ResourceType)i);
            return new ResourceSaveData { amounts = amounts };
        }

        public void RestoreState(ResourceSaveData data)
        {
            if (data?.amounts == null) return;

            int count = Mathf.Min(
                data.amounts.Length,
                Enum.GetValues(typeof(ResourceType)).Length);
            for (int i = 0; i < count; i++)
                SetAmount((ResourceType)i, Mathf.Max(0, data.amounts[i]));
            ResourcesChanged?.Invoke();
        }

        private void SetAmount(ResourceType type, int value)
        {
            switch (type)
            {
                case ResourceType.Food: food = value; break;
                case ResourceType.Water: water = value; break;
                case ResourceType.Medicine: medicine = value; break;
                case ResourceType.Parts: parts = value; break;
                case ResourceType.Battery: battery = value; break;
                case ResourceType.Fuel: fuel = value; break;
            }
            ResourcesChanged?.Invoke();
        }
    }
}
