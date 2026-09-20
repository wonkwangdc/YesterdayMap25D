using System;
using System.Collections.Generic;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Flow
{
    public sealed class BranchOneFlowController
    {
        private readonly HashSet<string> selectedEventIds = new(StringComparer.Ordinal);

        public event Action StateChanged;
        public event Action<BranchEventSelectionResult> EventSelected;
        public event Action<BranchDecisionResult> DecisionResolved;

        public BranchPrototypeSettings Settings { get; }
        public BranchEventCatalog EventCatalog { get; }
        public BranchRuntimeState RuntimeState { get; }
        public BranchScoreState ScoreState { get; }
        public BranchDiaryRepository DiaryRepository { get; }
        public BranchDecisionResolver DecisionResolver { get; }
        public BranchDecisionResult DecisionResult { get; private set; }
        public string LastFailureReason { get; private set; } = string.Empty;

        public BranchOneFlowController(
            BranchEventCatalog eventCatalog,
            BranchPrototypeSettings settings = null,
            BranchRuntimeState runtimeState = null,
            BranchScoreState scoreState = null,
            BranchDiaryRepository diaryRepository = null,
            BranchDecisionResolver decisionResolver = null)
        {
            EventCatalog = eventCatalog ?? throw new ArgumentNullException(nameof(eventCatalog));
            Settings = settings ?? new BranchPrototypeSettings();
            RuntimeState = runtimeState ?? new BranchRuntimeState();
            ScoreState = scoreState ?? new BranchScoreState();
            DiaryRepository = diaryRepository ?? new BranchDiaryRepository();
            DecisionResolver = decisionResolver ?? new BranchDecisionResolver();
        }

        public void StartTest()
        {
            ResetCore();
            ScoreState.Reset(Settings.StartingSignalScore, Settings.StartingJoinScore);
            RuntimeState.Initialize(Settings);
            DiaryRepository.GetOrCreateRecord(Settings.StartDay);
            NotifyStateChanged();
        }

        public BranchEventSelectionResult SelectEvent(string eventId, string choiceId)
        {
            if (!EventCatalog.TryGetById(eventId, out BranchEventDefinition definition))
            {
                return SelectionFailure($"Event ID was not found: {eventId}");
            }

            return SelectEvent(definition, choiceId);
        }

        public BranchEventSelectionResult SelectEvent(int eventNumber, string choiceId)
        {
            if (!EventCatalog.TryGetByNumber(eventNumber, out BranchEventDefinition definition))
            {
                return SelectionFailure($"Event number was not found: {eventNumber}");
            }

            return SelectEvent(definition, choiceId);
        }

        public bool AdvanceDay()
        {
            if (RuntimeState.Phase == BranchFlowPhase.NotStarted)
            {
                return FlowFailure("The test has not started.");
            }

            if (RuntimeState.CurrentDay >= Settings.DecisionDay)
            {
                return FlowFailure("The decision day cannot advance in this flow.");
            }

            if (Settings.RequireEventBeforeAdvance && !RuntimeState.IsTodayEventCompleted)
            {
                return FlowFailure("An event must be selected before advancing the day.");
            }

            if (RuntimeState.CurrentDay < Settings.LastEventDay)
            {
                int nextDay = RuntimeState.CurrentDay + 1;
                RuntimeState.BeginEventDay(nextDay);
                DiaryRepository.GetOrCreateRecord(nextDay);
                LastFailureReason = string.Empty;
                NotifyStateChanged();
                return true;
            }

            if (RuntimeState.CurrentDay == Settings.LastEventDay)
            {
                RuntimeState.BeginBranchResolution(Settings.DecisionDay);
                LastFailureReason = string.Empty;
                NotifyStateChanged();
                return true;
            }

            return FlowFailure("The current day is outside the event flow.");
        }

        public BranchDecisionResult ResolveDecision(double randomRoll)
        {
            if (DecisionResult != null || RuntimeState.IsDecisionCompleted)
            {
                return DecisionFailure(randomRoll, "The branch decision has already been resolved.");
            }

            if (RuntimeState.CurrentDay != Settings.DecisionDay ||
                RuntimeState.Phase != BranchFlowPhase.ResolvingBranch)
            {
                return DecisionFailure(randomRoll, "The flow is not ready to resolve the branch.");
            }

            BranchDecisionResult result = DecisionResolver.Resolve(
                ScoreState.SignalScore,
                ScoreState.JoinScore,
                randomRoll,
                Settings.WeightedRandomDecision);

            DecisionResult = result;
            RuntimeState.FinishDecision(result.DecidedRoute);
            LastFailureReason = result.FailureReason;
            DecisionResolved?.Invoke(result);
            NotifyStateChanged();
            return result;
        }

        public IReadOnlyList<BranchEventDefinition> GetAvailableEventsForCurrentDay()
        {
            if (RuntimeState.Phase != BranchFlowPhase.WaitingForEvent)
            {
                return Array.Empty<BranchEventDefinition>();
            }

            IReadOnlyList<BranchEventDefinition> available =
                EventCatalog.GetAvailableEvents(RuntimeState.CurrentDay);
            List<BranchEventDefinition> selectable = new();

            foreach (BranchEventDefinition definition in available)
            {
                if (CanRepeat(definition))
                {
                    selectable.Add(definition);
                }
            }

            return selectable.AsReadOnly();
        }

        public void Reset()
        {
            ResetCore();
            ScoreState.Reset(Settings.StartingSignalScore, Settings.StartingJoinScore);
            NotifyStateChanged();
        }

        public Quarter1SaveData CaptureState()
        {
            return new Quarter1SaveData
            {
                currentDay = RuntimeState.CurrentDay,
                phase = (int)RuntimeState.Phase,
                todaySelectedEventId = RuntimeState.TodaySelectedEventId,
                todayEventCompleted = RuntimeState.IsTodayEventCompleted,
                decisionCompleted = RuntimeState.IsDecisionCompleted,
                decidedRoute = (int)RuntimeState.DecidedRoute,
                signalScore = ScoreState.SignalScore,
                joinScore = ScoreState.JoinScore,
                selectedEventIds = new List<string>(selectedEventIds).ToArray(),
                diaryDays = DiaryRepository.CaptureState()
            };
        }

        public void RestoreState(Quarter1SaveData data)
        {
            if (data == null) return;

            ResetCore();
            ScoreState.Reset(data.signalScore, data.joinScore);
            RuntimeState.Restore(data);
            DiaryRepository.RestoreState(data.diaryDays);
            if (data.selectedEventIds != null)
            {
                foreach (string eventId in data.selectedEventIds)
                {
                    if (!string.IsNullOrWhiteSpace(eventId))
                        selectedEventIds.Add(eventId);
                }
            }

            if (data.decisionCompleted && RuntimeState.DecidedRoute != BranchRoute.None)
            {
                long total = (long)ScoreState.SignalScore + ScoreState.JoinScore;
                double signalRatio = total > 0 ? ScoreState.SignalScore / (double)total : 0d;
                double joinRatio = total > 0 ? ScoreState.JoinScore / (double)total : 0d;
                DecisionResult = BranchDecisionResult.Success(
                    ScoreState.SignalScore,
                    ScoreState.JoinScore,
                    signalRatio,
                    joinRatio,
                    0.5d,
                    RuntimeState.DecidedRoute);
            }

            NotifyStateChanged();
        }

        private BranchEventSelectionResult SelectEvent(
            BranchEventDefinition definition,
            string choiceId)
        {
            if (RuntimeState.Phase == BranchFlowPhase.NotStarted)
            {
                return SelectionFailure("The test has not started.", definition);
            }

            if (RuntimeState.CurrentDay >= Settings.DecisionDay ||
                RuntimeState.Phase == BranchFlowPhase.ResolvingBranch ||
                RuntimeState.Phase == BranchFlowPhase.Finished)
            {
                return SelectionFailure("Events cannot be selected on the decision day.", definition);
            }

            if (RuntimeState.IsTodayEventCompleted ||
                RuntimeState.Phase != BranchFlowPhase.WaitingForEvent)
            {
                return SelectionFailure("Only one event can be selected per day.", definition);
            }

            if (!definition.IsAvailableOnDay(RuntimeState.CurrentDay))
            {
                return SelectionFailure(
                    $"The event is not available on day {RuntimeState.CurrentDay}.",
                    definition);
            }

            if (!CanRepeat(definition))
            {
                return SelectionFailure("The event cannot be selected again.", definition);
            }

            if (!definition.TryGetChoice(choiceId, out BranchEventChoice choice))
            {
                return SelectionFailure($"Choice ID was not found: {choiceId}", definition);
            }

            int signalBefore = ScoreState.SignalScore;
            int joinBefore = ScoreState.JoinScore;
            ScoreState.AddSignalScore(choice.SignalScoreDelta);
            ScoreState.AddJoinScore(choice.JoinScoreDelta);
            int appliedSignalDelta = ScoreState.SignalScore - signalBefore;
            int appliedJoinDelta = ScoreState.JoinScore - joinBefore;

            string diaryEntry = string.IsNullOrWhiteSpace(choice.DiaryText)
                ? choice.ResultText
                : choice.DiaryText;
            DiaryRepository.AddEventRecord(RuntimeState.CurrentDay, diaryEntry);

            selectedEventIds.Add(definition.EventId);
            RuntimeState.CompleteEvent(definition.EventId);
            RuntimeState.MarkReadyToAdvance();
            LastFailureReason = string.Empty;

            BranchEventSelectionResult result = BranchEventSelectionResult.Success(
                definition,
                choice,
                appliedSignalDelta,
                appliedJoinDelta);
            EventSelected?.Invoke(result);
            NotifyStateChanged();
            return result;
        }

        private bool CanRepeat(BranchEventDefinition definition)
        {
            return !selectedEventIds.Contains(definition.EventId) ||
                   (Settings.AllowRepeatedEvents && definition.Repeatable);
        }

        private BranchEventSelectionResult SelectionFailure(
            string failureReason,
            BranchEventDefinition definition = null)
        {
            LastFailureReason = failureReason;
            return BranchEventSelectionResult.Failure(failureReason, definition);
        }

        private BranchDecisionResult DecisionFailure(double randomRoll, string failureReason)
        {
            LastFailureReason = failureReason;
            return BranchDecisionResult.Failure(
                ScoreState.SignalScore,
                ScoreState.JoinScore,
                NormalizeRandomRoll(randomRoll),
                failureReason);
        }

        private bool FlowFailure(string failureReason)
        {
            LastFailureReason = failureReason;
            return false;
        }

        private void ResetCore()
        {
            RuntimeState.Reset();
            DiaryRepository.Reset();
            selectedEventIds.Clear();
            DecisionResult = null;
            LastFailureReason = string.Empty;
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private static double NormalizeRandomRoll(double randomRoll)
        {
            if (double.IsNaN(randomRoll))
            {
                return 0.5d;
            }

            if (randomRoll <= 0d)
            {
                return 0d;
            }

            return randomRoll >= 1d ? 1d : randomRoll;
        }
    }
}
