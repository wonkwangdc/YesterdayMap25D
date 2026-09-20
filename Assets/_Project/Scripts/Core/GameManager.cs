using System;
using UnityEngine;
using YesterdayMap.Character;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private EndingManager endingManager;

        public GameState State { get; private set; } = GameState.Playing;
        public event Action<GameState> StateChanged;

        public void Configure(CharacterStats stats, EndingManager manager)
        {
            characterStats = stats;
            endingManager = manager;
        }

        private void OnEnable()
        {
            if (characterStats != null) characterStats.Died += HandleDeath;
        }

        private void OnDisable()
        {
            if (characterStats != null) characterStats.Died -= HandleDeath;
        }

        public void HandleDeath(string reason)
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            endingManager?.ShowGameOver(reason);
            StateChanged?.Invoke(State);
        }

        public void CompleteSurvival(bool rescued)
        {
            if (State != GameState.Playing) return;
            State = GameState.Ending;
            endingManager?.ShowSurvival(rescued);
            StateChanged?.Invoke(State);
        }

        public bool CompleteEnding(EndingId endingId)
        {
            if (endingId == EndingId.None ||
                (State == GameState.GameOver &&
                 endingId != EndingId.LastSurvivalRecord))
            {
                return false;
            }

            State = GameState.Ending;
            if (endingManager == null)
            {
                Debug.LogError($"Ending manager is missing. Ending: {endingId}");
                return false;
            }

            endingManager.ShowEnding(endingId);
            StateChanged?.Invoke(State);
            return true;
        }
    }
}
