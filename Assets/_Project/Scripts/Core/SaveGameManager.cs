using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using YesterdayMap.Character;
using YesterdayMap.BranchOne.Persistence;
using YesterdayMap.Events;
using YesterdayMap.Exploration;
using YesterdayMap.Resources;
using YesterdayMap.Shelter;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    // Owns the manual save slots and applies the selected save after Shelter has loaded.
    public sealed class SaveGameManager : MonoBehaviour
    {
        private const string SelectedSlotKey = "save.selectedSlot";
        private const string ShelterSceneName = "Shelter";

        public const int SlotCount = 5;

        private static SaveGameManager instance;
        private static SaveGameData pendingLoad;

        private DayCycleManager subscribedDayCycle;
        private bool isApplying;

        public static event Action<string> StatusChanged;
        public static string LastMessage { get; private set; } = string.Empty;
        public static int CurrentSlot => Mathf.Clamp(
            PlayerPrefs.GetInt(SelectedSlotKey, 1), 1, SlotCount);
        public static string SavePath => GetSavePath(CurrentSlot);
        public static bool HasSave => File.Exists(SavePath);
        public static bool HasAnySave
        {
            get
            {
                for (int slot = 1; slot <= SlotCount; slot++)
                {
                    if (File.Exists(GetSavePath(slot))) return true;
                }
                return false;
            }
        }

        public static bool HasSaveInSlot(int slot) =>
            File.Exists(GetSavePath(slot));

        public static string GetSavePath(int slot)
        {
            int safeSlot = Mathf.Clamp(slot, 1, SlotCount);
            return Path.Combine(
                Application.persistentDataPath,
                $"save_slot_{safeSlot:00}.json");
        }

        public static void SetCurrentSlot(int slot)
        {
            PlayerPrefs.SetInt(SelectedSlotKey, Mathf.Clamp(slot, 1, SlotCount));
            PlayerPrefs.Save();
        }

        public static string GetSlotSummary(int slot)
        {
            int safeSlot = Mathf.Clamp(slot, 1, SlotCount);
            if (!TryReadSave(safeSlot, out SaveGameData data, false))
                return $"{safeSlot}번 데이터 - 비어 있음";

            DateTime savedAt = GetSaveDateUtc(safeSlot, data).ToLocalTime();
            int day = data.dayCycle != null ? data.dayCycle.currentDay : 1;
            string playerName = data.session != null &&
                                !string.IsNullOrWhiteSpace(data.session.playerName)
                ? data.session.playerName
                : GameSession.DefaultPlayerName;
            return $"{safeSlot}번 데이터 - {playerName} - {day}일차 - {savedAt:MM/dd HH:mm}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            EnsureInstance();
        }

        private static SaveGameManager EnsureInstance()
        {
            if (instance != null) return instance;
            GameObject owner = new("SaveGameManager");
            instance = owner.AddComponent<SaveGameManager>();
            DontDestroyOnLoad(owner);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeAutoSave();
            instance = null;
        }

        public static bool SaveGame(bool automatic = false)
        {
            SaveGameManager manager = EnsureInstance();
            if (SceneManager.GetActiveScene().name != ShelterSceneName)
            {
                SetStatus("저장은 벙커 안에서만 할 수 있습니다.");
                return false;
            }

            try
            {
                SaveGameData data = manager.CaptureCurrentState();
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                SetStatus(automatic
                    ? $"{data.dayCycle.currentDay}일 차 자동 저장 완료"
                    : $"{data.dayCycle.currentDay}일 차 저장 완료");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus("저장에 실패했습니다. 콘솔을 확인해 주세요.");
                return false;
            }
        }

        public static bool LoadGame()
        {
            SaveGameManager manager = EnsureInstance();
            if (!TryReadSave(out SaveGameData data)) return false;

            pendingLoad = data;
            Time.timeScale = 1f;
            if (SceneManager.GetActiveScene().name == ShelterSceneName)
                manager.StartCoroutine(manager.ApplyPendingAfterSceneReady());
            else
                SceneManager.LoadScene(ShelterSceneName);
            return true;
        }

        public static bool LoadLatestGame()
        {
            int latestSlot = FindLatestSlot();
            if (latestSlot < 1)
            {
                SetStatus("저장된 게임이 없습니다.");
                return false;
            }

            SetCurrentSlot(latestSlot);
            return LoadGame();
        }

        public static bool TryReadSave(out SaveGameData data)
        {
            data = null;
            if (!HasSave)
            {
                SetStatus("저장된 게임이 없습니다.");
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveGameData>(
                    File.ReadAllText(SavePath));
                if (data == null || data.version > SaveGameData.CurrentVersion)
                {
                    SetStatus("현재 버전에서 읽을 수 없는 저장 파일입니다.");
                    data = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus("저장 파일을 읽지 못했습니다.");
                return false;
            }
        }

        public static bool TryReadSave(int slot, out SaveGameData data)
        {
            return TryReadSave(slot, out data, true);
        }

        private static bool TryReadSave(
            int slot,
            out SaveGameData data,
            bool reportErrors)
        {
            data = null;
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                if (reportErrors) SetStatus("선택한 슬롯에 저장된 게임이 없습니다.");
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
                if (data == null || data.version > SaveGameData.CurrentVersion)
                {
                    if (reportErrors)
                        SetStatus("현재 버전에서 읽을 수 없는 저장 파일입니다.");
                    data = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                if (reportErrors)
                {
                    Debug.LogException(exception);
                    SetStatus("저장 파일을 읽지 못했습니다.");
                }
                return false;
            }
        }

        private static int FindLatestSlot()
        {
            int latestSlot = -1;
            DateTime latestDate = DateTime.MinValue;
            for (int slot = 1; slot <= SlotCount; slot++)
            {
                if (!TryReadSave(slot, out SaveGameData data, false)) continue;
                DateTime date = GetSaveDateUtc(slot, data);
                if (latestSlot < 0 || date > latestDate)
                {
                    latestSlot = slot;
                    latestDate = date;
                }
            }
            return latestSlot;
        }

        private static DateTime GetSaveDateUtc(int slot, SaveGameData data)
        {
            if (data != null && DateTime.TryParse(
                    data.savedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsed))
            {
                return parsed.ToUniversalTime();
            }

            string path = GetSavePath(slot);
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != ShelterSceneName)
            {
                UnsubscribeAutoSave();
                return;
            }

            StartCoroutine(InitializeShelterAfterSceneReady());
        }

        private IEnumerator InitializeShelterAfterSceneReady()
        {
            yield return null;
            SubscribeAutoSave();
            if (pendingLoad != null)
                yield return ApplyPendingAfterSceneReady();
        }

        private IEnumerator ApplyPendingAfterSceneReady()
        {
            yield return null;
            if (pendingLoad == null || isApplying) yield break;

            isApplying = true;
            SaveGameData data = pendingLoad;
            pendingLoad = null;

            try
            {
                ResourceManager resources = FindFirstObjectByType<ResourceManager>();
                CharacterStats stats = FindFirstObjectByType<CharacterStats>();
                DayCycleManager dayCycle = FindFirstObjectByType<DayCycleManager>();
                GeneratorPowerManager generatorPower =
                    FindFirstObjectByType<GeneratorPowerManager>();
                ExplorationManager exploration = FindFirstObjectByType<ExplorationManager>();
                ShelterEventDialogueController campaign =
                    FindFirstObjectByType<ShelterEventDialogueController>();
                DiaryUI diary = FindFirstObjectByType<DiaryUI>(
                    FindObjectsInactive.Include);
                PlayerCarryController carryController =
                    FindFirstObjectByType<PlayerCarryController>();
                DayOneTutorialController tutorial =
                    FindFirstObjectByType<DayOneTutorialController>();

                resources?.RestoreState(data.resources);
                stats?.RestoreState(data.character);
                generatorPower?.RestoreState(data.generatorPower);
                GameSession.Instance?.RestoreState(data.session);
                exploration?.RestoreState(data.exploration);
                campaign?.RestoreState(data.campaign);
                diary?.RestoreState(data.diary);
                tutorial?.RestoreState(data.dayOneTutorial);
                RestoreShelterObjects(data.shelterObjects);

                if (stats != null)
                {
                    stats.transform.SetPositionAndRotation(
                        data.playerPosition,
                        data.playerRotation);
                    Rigidbody body = stats.GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }
                }

                bool hasCarriedFuel =
                    data.version >= 2 &&
                    data.fuelCan != null &&
                    data.fuelCan.isCarried;
                carryController?.RestoreFuelCanState(hasCarriedFuel);

                dayCycle?.RestoreState(data.dayCycle);
                SetStatus($"{data.dayCycle.currentDay}일 차 불러오기 완료");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus("불러오기에 실패했습니다. 콘솔을 확인해 주세요.");
            }
            finally
            {
                isApplying = false;
            }
        }

        private SaveGameData CaptureCurrentState()
        {
            ResourceManager resources = FindFirstObjectByType<ResourceManager>();
            CharacterStats stats = FindFirstObjectByType<CharacterStats>();
            DayCycleManager dayCycle = FindFirstObjectByType<DayCycleManager>();
            GeneratorPowerManager generatorPower =
                FindFirstObjectByType<GeneratorPowerManager>();
            ExplorationManager exploration = FindFirstObjectByType<ExplorationManager>();
            ShelterEventDialogueController campaign =
                FindFirstObjectByType<ShelterEventDialogueController>();
            DiaryUI diary = FindFirstObjectByType<DiaryUI>(
                FindObjectsInactive.Include);
            PlayerCarryController carryController =
                FindFirstObjectByType<PlayerCarryController>();
            DayOneTutorialController tutorial =
                FindFirstObjectByType<DayOneTutorialController>();

            if (resources == null || stats == null || dayCycle == null)
                throw new InvalidOperationException(
                    "Shelter save requires ResourceManager, CharacterStats, and DayCycleManager.");

            return new SaveGameData
            {
                version = SaveGameData.CurrentVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                sceneName = ShelterSceneName,
                playerPosition = stats.transform.position,
                playerRotation = stats.transform.rotation,
                dayCycle = dayCycle.CaptureState(),
                character = stats.CaptureState(),
                resources = resources.CaptureState(),
                generatorPower = generatorPower?.CaptureState() ??
                    new GeneratorPowerSaveData(),
                fuelCan = new FuelCanSaveData
                {
                    isCarried =
                        carryController != null &&
                        carryController.HasFuelCan
                },
                dayOneTutorial = tutorial?.CaptureState() ??
                    new DayOneTutorialSaveData(),
                session = GameSession.Instance?.CaptureState() ?? new GameSessionSaveData(),
                exploration = exploration?.CaptureState() ?? new ExplorationSaveData(),
                campaign = campaign?.CaptureState() ?? new CampaignSaveData(),
                diary = diary?.CaptureState() ?? new DiarySaveData(),
                shelterObjects = CaptureShelterObjects()
            };
        }

        private void SubscribeAutoSave()
        {
            DayCycleManager next = FindFirstObjectByType<DayCycleManager>();
            if (subscribedDayCycle == next) return;
            UnsubscribeAutoSave();
            subscribedDayCycle = next;
            if (subscribedDayCycle != null)
                subscribedDayCycle.DayChanged += HandleDayChanged;
        }

        private void UnsubscribeAutoSave()
        {
            if (subscribedDayCycle != null)
                subscribedDayCycle.DayChanged -= HandleDayChanged;
            subscribedDayCycle = null;
        }

        private void HandleDayChanged()
        {
            if (!isApplying)
                StartCoroutine(AutoSaveNextFrame());
        }

        private IEnumerator AutoSaveNextFrame()
        {
            yield return null;
            SaveGame(true);
        }

        private static ShelterObjectSaveData[] CaptureShelterObjects()
        {
            ShelterObject[] objects = FindObjectsByType<ShelterObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            List<ShelterObjectSaveData> result = new(objects.Length);
            foreach (ShelterObject shelterObject in objects)
            {
                result.Add(new ShelterObjectSaveData
                {
                    hierarchyPath = BuildHierarchyPath(shelterObject.transform),
                    isBroken = shelterObject.IsBroken
                });
            }
            return result.ToArray();
        }

        private static void RestoreShelterObjects(ShelterObjectSaveData[] data)
        {
            if (data == null) return;
            Dictionary<string, bool> saved = new(StringComparer.Ordinal);
            foreach (ShelterObjectSaveData item in data)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.hierarchyPath))
                    saved[item.hierarchyPath] = item.isBroken;
            }

            ShelterObject[] objects = FindObjectsByType<ShelterObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (ShelterObject shelterObject in objects)
            {
                if (saved.TryGetValue(
                        BuildHierarchyPath(shelterObject.transform),
                        out bool isBroken))
                    shelterObject.RestoreBrokenState(isBroken);
            }
        }

        private static string BuildHierarchyPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        private static void SetStatus(string message)
        {
            LastMessage = message ?? string.Empty;
            StatusChanged?.Invoke(LastMessage);
            Debug.Log($"[SaveGame] {LastMessage}");
        }
    }
}
