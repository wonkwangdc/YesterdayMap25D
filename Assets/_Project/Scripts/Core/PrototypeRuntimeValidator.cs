using System;
using UnityEngine;
using YesterdayMap.Character;
using YesterdayMap.Resources;
using YesterdayMap.Shelter;

namespace YesterdayMap.Core
{
    public sealed class PrototypeRuntimeValidator : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private DayCycleManager dayCycle;

        public void Configure(CharacterStats characterStats, ResourceManager resourceManager, DayCycleManager cycle)
        {
            stats = characterStats; resources = resourceManager; dayCycle = cycle;
        }

        private void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-prototypeSmokeTest") < 0) return;
            int facilityCount = FindObjectsByType<ShelterObject>(FindObjectsSortMode.None).Length;
            bool valid = stats != null && resources != null && dayCycle != null && facilityCount >= 8;
            Debug.Log(valid
                ? $"PROTOTYPE_SMOKE_TEST_PASS facilities={facilityCount} day={dayCycle.CurrentDay}"
                : $"PROTOTYPE_SMOKE_TEST_FAIL facilities={facilityCount}");
            Application.Quit(valid ? 0 : 1);
        }
    }
}
