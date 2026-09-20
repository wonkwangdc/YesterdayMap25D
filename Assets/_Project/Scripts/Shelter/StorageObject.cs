using UnityEngine;
using YesterdayMap.Audio;
using YesterdayMap.Character;
using YesterdayMap.Resources;
using YesterdayMap.UI;
using YesterdayMap.Core;

namespace YesterdayMap.Shelter
{
    public sealed class StorageObject : ShelterObject
    {
        private enum StorageUse
        {
            Combined,
            Food,
            Water,
            Medicine
        }

        [SerializeField] private ResourceManager resources;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private StorageUse storageUse;

        public override string InteractionPrompt => storageUse switch
        {
            StorageUse.Food => "[E] 식량 섭취",
            StorageUse.Water => "[E] 물 마시기",
            StorageUse.Medicine => "[E] 구급상자 사용",
            _ => "[E] 식량과 식수 섭취"
        };

        public override string AlternateInteractionPrompt => "[F] 창고 열기";
        public override bool CanAlternateInteract => true;

        public void Configure(ResourceManager manager, CharacterStats characterStats)
        {
            resources = manager;
            stats = characterStats;
        }

        public override void Interact()
        {
            ResolveReferences();
            if (resources == null || stats == null)
            {
                Ui?.ShowMessage("물자 정보를 찾을 수 없습니다.");
                return;
            }

            switch (storageUse)
            {
                case StorageUse.Food:
                    ConsumeFood();
                    break;
                case StorageUse.Water:
                    ConsumeWater();
                    break;
                case StorageUse.Medicine:
                    UseMedicine();
                    break;
                default:
                    ConsumeCombined();
                    break;
            }
        }

        public override void AlternateInteract()
        {
            Ui?.OpenStorage();
        }

        public static void InstallShelfInteractions(
            ResourceManager manager,
            Transform foodShelf,
            Transform waterShelf,
            Transform medicineShelf)
        {
            if (manager == null || foodShelf == null || waterShelf == null)
            {
                return;
            }

            var shelfScene = foodShelf.gameObject.scene;
            CharacterStats characterStats = FindInScene<CharacterStats>(shelfScene);
            UIManager ui = FindInScene<UIManager>(shelfScene);
            if (characterStats == null || ui == null)
            {
                Debug.LogWarning("선반 상호작용에 필요한 캐릭터 또는 UI를 찾지 못했습니다.");
                return;
            }

            EnsureShelfInteraction(
                foodShelf,
                manager,
                characterStats,
                ui,
                StorageUse.Food);
            EnsureShelfInteraction(
                waterShelf,
                manager,
                characterStats,
                ui,
                StorageUse.Water);

            if (medicineShelf != null)
            {
                EnsureShelfInteraction(
                    medicineShelf,
                    manager,
                    characterStats,
                    ui,
                    StorageUse.Medicine);
            }

            StorageObject[] storageObjects = FindObjectsByType<StorageObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (StorageObject storage in storageObjects)
            {
                if (storage == null ||
                    storage.gameObject.scene != shelfScene ||
                    storage.storageUse != StorageUse.Combined ||
                    storage.gameObject.name != "Storage")
                {
                    continue;
                }

                Collider legacyTrigger = storage.GetComponent<Collider>();
                if (legacyTrigger != null)
                {
                    legacyTrigger.enabled = false;
                }

                storage.enabled = false;
            }
        }

        private static void EnsureShelfInteraction(
            Transform shelf,
            ResourceManager manager,
            CharacterStats characterStats,
            UIManager ui,
            StorageUse use)
        {
            StorageObject interaction = shelf.GetComponent<StorageObject>();
            if (interaction == null)
            {
                interaction = shelf.gameObject.AddComponent<StorageObject>();
            }

            interaction.resources = manager;
            interaction.stats = characterStats;
            interaction.storageUse = use;
            interaction.ConfigureBase(ui, interaction.InteractionPrompt);
            interaction.enabled = true;
        }

        private static T FindInScene<T>(
            UnityEngine.SceneManagement.Scene scene)
            where T : Component
        {
            T[] candidates = FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (T candidate in candidates)
            {
                if (candidate != null && candidate.gameObject.scene == scene)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ConsumeFood()
        {
            if (stats.Hunger >= 99.9f)
            {
                Ui?.ShowMessage("배고픔이 이미 가득 찼습니다.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Food, 1))
            {
                Ui?.ShowMessage("먹을 식량이 없습니다.");
                return;
            }

            stats.ModifyHunger(25f);
            GameSession.Instance?.RecordFoodConsumed();
            DayOneTutorialController.Instance?.NotifyFoodConsumed();
            ConsumableUseVisual.Play(ConsumableVisualKind.Food);
            Ui?.ShowMessage("식량을 섭취했습니다.");
            GameSfxPlayer.Play(GameSfxCue.UseItem);
        }

        private void ConsumeWater()
        {
            if (stats.Thirst >= 99.9f)
            {
                Ui?.ShowMessage("갈증이 이미 가득 찼습니다.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Water, 1))
            {
                Ui?.ShowMessage("마실 물이 없습니다.");
                return;
            }

            stats.ModifyThirst(30f);
            GameSession.Instance?.RecordWaterConsumed();
            DayOneTutorialController.Instance?.NotifyWaterConsumed();
            ConsumableUseVisual.Play(ConsumableVisualKind.Water);
            Ui?.ShowMessage("물을 마셨습니다.");
            GameSfxPlayer.Play(GameSfxCue.DrinkWater);
        }

        private void UseMedicine()
        {
            if (stats.Health >= 99.9f)
            {
                Ui?.ShowMessage("체력이 이미 가득 찼습니다.");
                return;
            }

            if (!resources.TrySpend(ResourceType.Medicine, 1))
            {
                Ui?.ShowMessage("사용할 구급상자가 없습니다.");
                return;
            }

            stats.ModifyHealth(40f);
            Ui?.ShowMessage("구급상자를 사용해 체력을 회복했습니다.");
            GameSfxPlayer.Play(GameSfxCue.UseItem);
        }

        private void ConsumeCombined()
        {
            bool needsFood = stats.Hunger < 99.9f;
            bool needsWater = stats.Thirst < 99.9f;
            if (!needsFood && !needsWater)
            {
                Ui?.ShowMessage("배고픔과 갈증이 이미 가득 찼습니다.");
                return;
            }

            if (needsFood && !resources.Has(ResourceType.Food, 1))
            {
                Ui?.ShowMessage("먹을 식량이 없습니다.");
                return;
            }

            if (needsWater && !resources.Has(ResourceType.Water, 1))
            {
                Ui?.ShowMessage("마실 물이 없습니다.");
                return;
            }

            if (needsFood)
            {
                resources.TrySpend(ResourceType.Food, 1);
                stats.ModifyHunger(25f);
                GameSession.Instance?.RecordFoodConsumed();
            }

            if (needsWater)
            {
                resources.TrySpend(ResourceType.Water, 1);
                stats.ModifyThirst(30f);
                GameSession.Instance?.RecordWaterConsumed();
            }

            ConsumableUseVisual.Play(needsFood, needsWater);

            Ui?.ShowMessage(needsFood && needsWater
                ? "식량과 식수를 섭취했습니다."
                : needsFood
                    ? "식량을 섭취했습니다."
                    : "물을 마셨습니다.");
            GameSfxPlayer.Play(needsFood
                ? GameSfxCue.UseItem
                : GameSfxCue.DrinkWater);
        }

        private void ResolveReferences()
        {
            if (resources == null)
            {
                resources = FindFirstObjectByType<ResourceManager>();
            }

            if (stats == null)
            {
                stats = FindFirstObjectByType<CharacterStats>();
            }
        }
    }
}
