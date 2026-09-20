using System;
using UnityEngine;
using YesterdayMap.Character;
using YesterdayMap.Events;
using YesterdayMap.Resources;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    // 날짜와 당일 벙커 상태를 묶어 하루의 흐름을 책임진다.
    public sealed class DayCycleManager : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private EventManager eventManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIManager ui;
        public event Action DayChanged;
        public event Action DayAdvanced;
        public int CurrentDay { get; private set; } = 1;

        public void Configure(CharacterStats characterStats, ResourceManager resourceManager,
            EventManager events, GameManager manager, UIManager uiManager)
        {
            stats = characterStats;
            resources = resourceManager;
            eventManager = events;
            gameManager = manager;
            ui = uiManager;
        }

        public void EndDay()
        {
            // 수면 변화 -> 야간 사건 -> 다음 날 순서는 BedObject와 함께 유지한다.
            if (gameManager.State != GameState.Playing) return;
            eventManager.ResolveNightEvent();
            if (gameManager.State != GameState.Playing) return;

            int warningCountBeforeMorning = stats.SurvivalRecords.Count;
            CurrentDay++;
            DayAdvanced?.Invoke();
            stats.ProcessMorning(CurrentDay);
            if (gameManager.State != GameState.Playing) return;
            // 날짜 자체는 엔딩 조건이 아니다. 스토리 선택이나 생존 실패가 게임 종료를 결정한다.
            DayChanged?.Invoke();
            ui.ShowMessage(stats.SurvivalRecords.Count > warningCountBeforeMorning
                ? stats.LatestSurvivalRecord
                : $"{CurrentDay}일차 아침입니다. 벙커 상태를 확인하세요.");
        }

        public void SetDayForTesting(int day)
        {
            CurrentDay = Mathf.Max(1, day);
            DayChanged?.Invoke();
            ui?.ShowMessage($"[테스트] {CurrentDay}일차로 이동했습니다.");
        }

        public DayCycleSaveData CaptureState()
        {
            return new DayCycleSaveData
            {
                currentDay = CurrentDay
            };
        }

        public void RestoreState(DayCycleSaveData data)
        {
            if (data == null) return;

            CurrentDay = Mathf.Max(1, data.currentDay);
            DayChanged?.Invoke();
        }
    }
}
