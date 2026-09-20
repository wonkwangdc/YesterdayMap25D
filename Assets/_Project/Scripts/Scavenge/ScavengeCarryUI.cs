using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Resources;

namespace YesterdayMap.Scavenge
{
    // 수집 규칙은 ScavengeManager가 관리하고, 이 스크립트는 하단 아이콘 표시만 담당한다.
    public sealed class ScavengeCarryUI : MonoBehaviour
    {
        private const string FoodIconPath = "UI/CarryIcons/Food";
        private const string WaterIconPath = "UI/CarryIcons/Water";
        private const string MedicineIconPath = "UI/CarryIcons/Medicine";
        private const string KitchenKnifeIconPath = "UI/CarryIcons/KitchenKnife";
        private const string TeddyBearIconPath = "UI/CarryIcons/TeddyBear";
        private const string ClothesIconPath = "UI/CarryIcons/Clothes";
        private const string SoccerBallIconPath = "UI/CarryIcons/SoccerBall";
        private const string BaseballBatIconPath = "UI/CarryIcons/BaseballBat";
        private const string GuitarIconPath = "UI/CarryIcons/Guitar";

        [SerializeField] private ScavengeManager manager;
        [SerializeField] private Texture2D itemAtlas;
        [SerializeField] private RawImage[] itemSlots;
        [SerializeField] private Text capacityText;
        [Header("Optional icon overrides")]
        [SerializeField] private Texture2D foodIcon;
        [SerializeField] private Texture2D waterIcon;
        [SerializeField] private Texture2D medicineIcon;
        [SerializeField] private Texture2D kitchenKnifeIcon;
        [SerializeField] private Texture2D teddyBearIcon;
        [SerializeField] private Texture2D clothesIcon;
        [SerializeField] private Texture2D soccerBallIcon;
        [SerializeField] private Texture2D baseballBatIcon;
        [SerializeField] private Texture2D guitarIcon;

        public void Configure(ScavengeManager scavengeManager, Texture2D atlas, RawImage[] slots, Text label)
        {
            Unsubscribe();
            manager = scavengeManager;
            itemAtlas = atlas;
            itemSlots = slots;
            capacityText = label;
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (manager != null)
            {
                manager.CarriedItemsChanged -= Refresh;
                manager.CarriedItemsChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (manager != null)
            {
                manager.CarriedItemsChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            if (manager == null || itemSlots == null)
            {
                return;
            }

            for (int i = 0; i < itemSlots.Length; i++)
            {
                RawImage slot = itemSlots[i];
                bool hasItem = i < manager.CarriedItems.Count;
                slot.enabled = hasItem;

                if (hasItem)
                {
                    ResourceType itemType = manager.CarriedItems[i];
                    ScavengeItemKind itemKind = i < manager.CarriedItemKinds.Count
                        ? manager.CarriedItemKinds[i]
                        : ScavengeItemKind.Resource;
                    Texture2D itemIcon = GetItemIcon(itemType, itemKind);
                    slot.texture = itemIcon != null ? itemIcon : itemAtlas;
                    slot.uvRect = itemIcon != null
                        ? new Rect(0f, 0f, 1f, 1f)
                        : GetAtlasUv(itemType);
                    slot.color = Color.white;
                }
            }

            if (capacityText != null)
            {
                capacityText.text = $"운반 물자  {manager.CarriedItems.Count}/{manager.MaximumItems}";
            }
        }

        // UI 전용 이미지는 Resources에서 한 번만 불러온다.
        // 수집 규칙은 ScavengeManager에 남겨 두어 게임 데이터와 화면 표시가 섞이지 않게 한다.
        private Texture2D GetItemIcon(ResourceType type, ScavengeItemKind itemKind)
        {
            switch (itemKind)
            {
                case ScavengeItemKind.KitchenKnife:
                    return LoadIcon(ref kitchenKnifeIcon, KitchenKnifeIconPath);
                case ScavengeItemKind.TeddyBear:
                    return LoadIcon(ref teddyBearIcon, TeddyBearIconPath);
                case ScavengeItemKind.Clothes:
                    return LoadIcon(ref clothesIcon, ClothesIconPath);
                case ScavengeItemKind.SoccerBall:
                    return LoadIcon(ref soccerBallIcon, SoccerBallIconPath);
                case ScavengeItemKind.BaseballBat:
                    return LoadIcon(ref baseballBatIcon, BaseballBatIconPath);
                case ScavengeItemKind.Guitar:
                    return LoadIcon(ref guitarIcon, GuitarIconPath);
            }

            switch (type)
            {
                case ResourceType.Water:
                    return LoadIcon(ref waterIcon, WaterIconPath);
                case ResourceType.Medicine:
                    return LoadIcon(ref medicineIcon, MedicineIconPath);
                default:
                    return LoadIcon(ref foodIcon, FoodIconPath);
            }
        }

        private static Texture2D LoadIcon(ref Texture2D icon, string path)
        {
            if (icon == null)
            {
                icon = UnityEngine.Resources.Load<Texture2D>(path);
            }

            return icon;
        }

        // 아이콘 시트는 왼쪽부터 식량, 식수, 구급상자 순서다.
        private static Rect GetAtlasUv(ResourceType type)
        {
            int cell = type switch
            {
                ResourceType.Water => 1,
                ResourceType.Medicine => 2,
                _ => 0
            };

            return new Rect(cell / 3f, 0f, 1f / 3f, 1f);
        }
    }
}
