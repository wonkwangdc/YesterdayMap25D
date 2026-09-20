using System;

namespace YesterdayMap.BranchOne.Events
{
    [Serializable]
    public sealed class BranchEventChoice
    {
        public string ChoiceId { get; }
        public string Label { get; }
        public string ResultText { get; }
        public int SignalScoreDelta { get; }
        public int JoinScoreDelta { get; }
        public string DiaryText { get; }

        public BranchEventChoice(
            string choiceId,
            string label,
            string resultText,
            int signalScoreDelta,
            int joinScoreDelta,
            string diaryText)
        {
            ChoiceId = choiceId ?? string.Empty;
            Label = label ?? string.Empty;
            ResultText = resultText ?? string.Empty;
            SignalScoreDelta = signalScoreDelta;
            JoinScoreDelta = joinScoreDelta;
            DiaryText = diaryText ?? string.Empty;
        }

        public bool TryValidate(out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(ChoiceId))
            {
                failureReason = "Choice ID cannot be empty.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
