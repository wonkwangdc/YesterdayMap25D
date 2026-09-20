using UnityEngine;
using YesterdayMap.Resources;
using YesterdayMap.Shelter;

namespace YesterdayMap.Core
{
    public sealed class ShelterBootstrap : MonoBehaviour
    {
        [SerializeField] private ResourceManager resources;
        public void Configure(ResourceManager manager) => resources = manager;
        private void Start()
        {
            Time.timeScale = 1f;
            GameSession.Instance?.TransferCollectedTo(resources);
            ShelterResourceDisplay.Ensure(resources, 0, 0);
            DayOneTutorialController.Ensure(
                gameObject,
                FindFirstObjectByType<DayCycleManager>(),
                FindFirstObjectByType<YesterdayMap.UI.UIManager>());
        }
    }
}
