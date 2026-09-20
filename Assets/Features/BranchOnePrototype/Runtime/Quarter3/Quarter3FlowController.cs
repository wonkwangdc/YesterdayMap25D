using System;
using System.Collections.Generic;

namespace YesterdayMap.BranchOne.Quarter3
{
    public sealed class Quarter3FlowController
    {
        public Quarter3ClueCatalog ClueCatalog { get; }
        public Quarter3FinalChoiceCatalog FinalChoiceCatalog { get; }
        public Quarter3RuntimeState RuntimeState { get; }
        public string LastFailureReason { get; private set; } = string.Empty;
        public Quarter3FlowPhase Phase => RuntimeState.Phase;
        public BranchRoute CurrentRoute => RuntimeState.CurrentRoute;
        public bool IsActive => RuntimeState.IsActive;
        public bool IsFinalChoiceOpen => RuntimeState.IsFinalChoiceOpen;
        public bool IsResolved => RuntimeState.IsResolved;
        public IReadOnlyList<string> AcquiredClueIds =>
            RuntimeState.AcquiredClueIds;
        public string SelectedFinalChoiceId =>
            RuntimeState.SelectedFinalChoiceId;

        public Quarter3FlowController(
            Quarter3ClueCatalog clueCatalog,
            Quarter3FinalChoiceCatalog finalChoiceCatalog,
            Quarter3RuntimeState runtimeState = null)
        {
            ClueCatalog = clueCatalog ??
                throw new ArgumentNullException(nameof(clueCatalog));
            FinalChoiceCatalog = finalChoiceCatalog ??
                throw new ArgumentNullException(nameof(finalChoiceCatalog));
            RuntimeState = runtimeState ?? new Quarter3RuntimeState();
        }

        public bool StartQuarter3(BranchRoute passedRoute)
        {
            if (Phase != Quarter3FlowPhase.NotStarted)
            {
                return FlowFailure("Quarter 3 has already started.");
            }

            if (!IsPlayableRoute(passedRoute))
            {
                return FlowFailure(
                    "Quarter 3 requires the Signal or Join route.");
            }

            if (!FinalChoiceCatalog.TryValidate(out string failureReason))
            {
                return FlowFailure(failureReason);
            }

            RuntimeState.Start(passedRoute);
            LastFailureReason = string.Empty;
            return true;
        }

        public IReadOnlyList<Quarter3ClueDefinition> GetAvailableClues(
            string sourceId)
        {
            if (!IsActive || string.IsNullOrWhiteSpace(sourceId))
            {
                return Array.Empty<Quarter3ClueDefinition>();
            }

            IReadOnlyList<Quarter3ClueDefinition> sourceClues =
                ClueCatalog.GetClues(CurrentRoute, sourceId);
            List<Quarter3ClueDefinition> availableClues = new();

            foreach (Quarter3ClueDefinition clue in sourceClues)
            {
                if (!RuntimeState.HasClue(clue.ClueId))
                {
                    availableClues.Add(clue);
                }
            }

            return availableClues.AsReadOnly();
        }

        public Quarter3ClueAcquisitionResult TryAcquireClue(string clueId)
        {
            if (Phase == Quarter3FlowPhase.NotStarted)
            {
                return ClueFailure(
                    Quarter3ClueAcquisitionStatus.NotStarted,
                    clueId,
                    null,
                    "Quarter 3 has not started.");
            }

            if (Phase == Quarter3FlowPhase.Resolved)
            {
                return ClueFailure(
                    Quarter3ClueAcquisitionStatus.Resolved,
                    clueId,
                    null,
                    "Quarter 3 has already been resolved.");
            }

            if (string.IsNullOrWhiteSpace(clueId))
            {
                return ClueFailure(
                    Quarter3ClueAcquisitionStatus.InvalidClueId,
                    clueId,
                    null,
                    "Quarter 3 clue ID cannot be empty.");
            }

            if (!ClueCatalog.TryGetById(
                    clueId,
                    out Quarter3ClueDefinition definition))
            {
                return ClueFailure(
                    Quarter3ClueAcquisitionStatus.ClueNotRegistered,
                    clueId,
                    null,
                    $"Quarter 3 clue is not registered: {clueId}");
            }

            if (definition.Route != CurrentRoute)
            {
                return ClueFailure(
                    Quarter3ClueAcquisitionStatus.RouteMismatch,
                    clueId,
                    definition,
                    "The Quarter 3 clue belongs to a different route.");
            }

            if (RuntimeState.HasClue(clueId))
            {
                return ClueFailure(
                    Quarter3ClueAcquisitionStatus.AlreadyAcquired,
                    clueId,
                    definition,
                    "The Quarter 3 clue has already been acquired.");
            }

            RuntimeState.AddClue(clueId);
            LastFailureReason = string.Empty;
            return Quarter3ClueAcquisitionResult.Success(
                clueId,
                definition);
        }

        public bool HasClue(string clueId)
        {
            return RuntimeState.HasClue(clueId);
        }

        public IReadOnlyList<Quarter3ClueDefinition> GetAcquiredClues()
        {
            List<Quarter3ClueDefinition> acquiredClues = new();
            foreach (string clueId in AcquiredClueIds)
            {
                if (ClueCatalog.TryGetById(
                        clueId,
                        out Quarter3ClueDefinition definition))
                {
                    acquiredClues.Add(definition);
                }
            }

            return acquiredClues.AsReadOnly();
        }

        public Quarter3SourceProgress GetSourceProgress(string sourceId)
        {
            if (!IsPlayableRoute(CurrentRoute) ||
                string.IsNullOrWhiteSpace(sourceId))
            {
                return new Quarter3SourceProgress(
                    sourceId,
                    0,
                    0);
            }

            IReadOnlyList<Quarter3ClueDefinition> sourceClues =
                ClueCatalog.GetClues(CurrentRoute, sourceId);
            int acquiredCount = 0;
            foreach (Quarter3ClueDefinition clue in sourceClues)
            {
                if (RuntimeState.HasClue(clue.ClueId))
                {
                    acquiredCount++;
                }
            }

            return new Quarter3SourceProgress(
                sourceId,
                sourceClues.Count,
                acquiredCount);
        }

        public IReadOnlyList<Quarter3FinalChoiceDefinition>
            GetFinalChoiceOptions()
        {
            return IsPlayableRoute(CurrentRoute)
                ? FinalChoiceCatalog.GetChoices(CurrentRoute)
                : Array.Empty<Quarter3FinalChoiceDefinition>();
        }

        public bool OpenFinalChoice()
        {
            if (Phase != Quarter3FlowPhase.CollectingClues)
            {
                return FlowFailure(
                    "The Quarter 3 final choice can only be opened " +
                    "while collecting clues.");
            }

            RuntimeState.OpenFinalChoice();
            LastFailureReason = string.Empty;
            return true;
        }

        public Quarter3FinalSelectionResult SelectFinalChoice(
            string finalChoiceId)
        {
            if (Phase == Quarter3FlowPhase.NotStarted)
            {
                return FinalSelectionFailure(
                    Quarter3FinalSelectionStatus.NotStarted,
                    finalChoiceId,
                    null,
                    "Quarter 3 has not started.");
            }

            if (Phase == Quarter3FlowPhase.Resolved)
            {
                return FinalSelectionFailure(
                    Quarter3FinalSelectionStatus.AlreadyResolved,
                    finalChoiceId,
                    null,
                    "Quarter 3 has already been resolved.");
            }

            if (Phase != Quarter3FlowPhase.FinalChoiceOpen)
            {
                return FinalSelectionFailure(
                    Quarter3FinalSelectionStatus.FinalChoiceNotOpen,
                    finalChoiceId,
                    null,
                    "The Quarter 3 final choice is not open.");
            }

            if (string.IsNullOrWhiteSpace(finalChoiceId))
            {
                return FinalSelectionFailure(
                    Quarter3FinalSelectionStatus.InvalidFinalChoiceId,
                    finalChoiceId,
                    null,
                    "Quarter 3 final choice ID cannot be empty.");
            }

            if (!FinalChoiceCatalog.TryGetById(
                    finalChoiceId,
                    out Quarter3FinalChoiceDefinition definition))
            {
                return FinalSelectionFailure(
                    Quarter3FinalSelectionStatus.FinalChoiceNotRegistered,
                    finalChoiceId,
                    null,
                    $"Quarter 3 final choice is not registered: " +
                    finalChoiceId);
            }

            if (definition.Route != CurrentRoute)
            {
                return FinalSelectionFailure(
                    Quarter3FinalSelectionStatus.RouteMismatch,
                    finalChoiceId,
                    definition,
                    "The Quarter 3 final choice belongs to a different route.");
            }

            RuntimeState.Resolve(finalChoiceId);
            LastFailureReason = string.Empty;
            return Quarter3FinalSelectionResult.Success(
                finalChoiceId,
                definition);
        }

        public void Reset()
        {
            RuntimeState.Reset();
            LastFailureReason = string.Empty;
        }

        private Quarter3ClueAcquisitionResult ClueFailure(
            Quarter3ClueAcquisitionStatus status,
            string clueId,
            Quarter3ClueDefinition definition,
            string failureReason)
        {
            LastFailureReason = failureReason;
            return Quarter3ClueAcquisitionResult.Failure(
                status,
                clueId,
                definition,
                failureReason);
        }

        private Quarter3FinalSelectionResult FinalSelectionFailure(
            Quarter3FinalSelectionStatus status,
            string finalChoiceId,
            Quarter3FinalChoiceDefinition definition,
            string failureReason)
        {
            LastFailureReason = failureReason;
            return Quarter3FinalSelectionResult.Failure(
                status,
                finalChoiceId,
                definition,
                failureReason);
        }

        private bool FlowFailure(string failureReason)
        {
            LastFailureReason = failureReason;
            return false;
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }
    }
}
