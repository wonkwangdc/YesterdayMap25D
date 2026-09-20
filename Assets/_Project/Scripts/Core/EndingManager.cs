using UnityEngine;
using YesterdayMap.Audio;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    public sealed class EndingManager : MonoBehaviour
    {
        [SerializeField] private EndingUI endingUI;

        public void Configure(EndingUI ui) => endingUI = ui;

        public void ShowGameOver(string reason)
        {
            Time.timeScale = 0f;
            LastSurvivalRecordEndingSequence sequence =
                FindFirstObjectByType<LastSurvivalRecordEndingSequence>(
                    FindObjectsInactive.Include);
            if (sequence != null && sequence.Play(reason))
            {
                return;
            }

            if (endingUI == null)
            {
                Debug.LogError($"Ending UI is missing. Game over reason: {reason}");
                return;
            }

            endingUI.ShowGameOver(reason);
        }

        public void ShowSurvival(bool rescued)
        {
            Time.timeScale = 0f;
            ShowEnding(rescued ? EndingId.LastFrequency : EndingId.WithStrangers);
        }

        public void ShowEnding(EndingId endingId)
        {
            Time.timeScale = 0f;
            if (endingUI == null)
            {
                Debug.LogError($"Ending UI is missing. Ending: {endingId}");
                return;
            }

            (string title, string detail) = GetPresentation(endingId);
            endingUI.ShowEnding(title, detail);
            if (Application.isPlaying)
            {
                GameSfxPlayer.Play(GameSfxCue.Ending);
            }
        }

        private static (string Title, string Detail) GetPresentation(EndingId endingId)
        {
            if (endingId == EndingId.LastBunker)
            {
                return ("마지막 벙커", string.Empty);
            }

            if (endingId == EndingId.LastSurvivalRecord)
            {
                return ("마지막 생존 기록", string.Empty);
            }

            return endingId switch
            {
                EndingId.LastFrequency => (
                    "마지막 주파수",
                    string.Empty),
                EndingId.DoorOpenedNight => (
                    "마지막 유인 방송",
                    string.Empty),
                EndingId.WithStrangers => (
                    "낯선 사람들과",
                    string.Empty),
                EndingId.RedArmband => (
                    "붉은 완장",
                    string.Empty),
                EndingId.LastBunker => (
                    "마지막 벙커",
                    "긴 시간이 흐른 뒤에도 마지막 기록은 벙커에 남았다."),
                EndingId.LastSurvivalRecord => (
                    "마지막 생존 기록",
                    "생존자의 마지막 기록은 여기에서 끝났다."),
                _ => (
                    "기록되지 않은 결말",
                    "최종 선택을 확인할 수 없습니다.")
            };
        }
    }
}
