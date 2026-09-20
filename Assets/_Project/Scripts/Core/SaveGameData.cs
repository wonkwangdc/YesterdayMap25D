using System;
using UnityEngine;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.Core
{
    [Serializable]
    public sealed class SaveGameData
    {
        public const int CurrentVersion = 4;

        public int version = CurrentVersion;
        public string savedAtUtc = string.Empty;
        public string sceneName = "Shelter";
        public Vector3 playerPosition;
        public Quaternion playerRotation = Quaternion.identity;
        public DayCycleSaveData dayCycle = new();
        public CharacterStatsSaveData character = new();
        public ResourceSaveData resources = new();
        public GeneratorPowerSaveData generatorPower = new();
        public FuelCanSaveData fuelCan = new();
        public DayOneTutorialSaveData dayOneTutorial = new();
        public GameSessionSaveData session = new();
        public ExplorationSaveData exploration = new();
        public CampaignSaveData campaign = new();
        public DiarySaveData diary = new();
        public ShelterObjectSaveData[] shelterObjects = Array.Empty<ShelterObjectSaveData>();
    }

    [Serializable]
    public sealed class DayCycleSaveData
    {
        public int currentDay = 1;
    }

    [Serializable]
    public sealed class CharacterStatsSaveData
    {
        public float health = 100f;
        public float hunger = 85f;
        public float thirst = 85f;
        public float morale = 75f;
        public int hungerWarningStage;
        public int thirstWarningStage;
        public int zeroSequence;
        public int hungerZeroSequence = int.MaxValue;
        public int thirstZeroSequence = int.MaxValue;
        public bool healthAwaitingMorning;
        public bool isCriticalCondition;
        public bool deathResolved;
        public string[] survivalRecords = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ResourceSaveData
    {
        public int[] amounts = Array.Empty<int>();
    }

    [Serializable]
    public sealed class GeneratorPowerSaveData
    {
        public float powerPercent = 100f;
        public bool isRunning = true;
    }

    [Serializable]
    public sealed class FuelCanSaveData
    {
        public bool isCarried;
    }

    [Serializable]
    public sealed class DayOneTutorialSaveData
    {
        public bool introPlayed;
        public bool diaryOpened;
        public bool storageOpened;
        public bool foodConsumed;
        public bool waterConsumed;
        public bool generatorFueled;
        public bool sleepGuideShown;
        public bool completed;
    }

    [Serializable]
    public sealed class GameSessionSaveData
    {
        public string playerName = string.Empty;
        public int[] collectedResources = Array.Empty<int>();
        public int[] collectedItems = Array.Empty<int>();
        public int[] lastMoraleItemUseDays = Array.Empty<int>();
        public float elapsedPlaySeconds;
        public int foodConsumedCount;
        public int waterConsumedCount;
    }

    [Serializable]
    public sealed class DiarySaveData
    {
        public bool hasUnreadResult;
        public bool rebuildUnreadMainDiaryPages;
        public int[] unreadMainDiaryPages = Array.Empty<int>();
    }

    [Serializable]
    public sealed class ExplorationSaveData
    {
        public int lastExplorationDay = -1;
        public string[] history = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ShelterObjectSaveData
    {
        public string hierarchyPath = string.Empty;
        public bool isBroken;
    }

}
