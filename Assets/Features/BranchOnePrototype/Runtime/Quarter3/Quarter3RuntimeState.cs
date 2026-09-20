using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Quarter3
{
    [Serializable]
    public sealed class Quarter3RuntimeState
    {
        private readonly List<string> acquiredClueIds = new();
        private readonly ReadOnlyCollection<string> readOnlyAcquiredClueIds;
        private readonly HashSet<string> acquiredClueIdSet =
            new(StringComparer.Ordinal);

        public Quarter3FlowPhase Phase { get; private set; } =
            Quarter3FlowPhase.NotStarted;
        public BranchRoute CurrentRoute { get; private set; } =
            BranchRoute.None;
        public IReadOnlyList<string> AcquiredClueIds =>
            readOnlyAcquiredClueIds;
        public string SelectedFinalChoiceId { get; private set; } =
            string.Empty;
        public bool IsActive =>
            Phase == Quarter3FlowPhase.CollectingClues ||
            Phase == Quarter3FlowPhase.FinalChoiceOpen;
        public bool IsFinalChoiceOpen =>
            Phase == Quarter3FlowPhase.FinalChoiceOpen;
        public bool IsResolved => Phase == Quarter3FlowPhase.Resolved;

        public Quarter3RuntimeState()
        {
            readOnlyAcquiredClueIds = acquiredClueIds.AsReadOnly();
        }

        public bool HasClue(string clueId)
        {
            return !string.IsNullOrWhiteSpace(clueId) &&
                   acquiredClueIdSet.Contains(clueId);
        }

        public void Reset()
        {
            Phase = Quarter3FlowPhase.NotStarted;
            CurrentRoute = BranchRoute.None;
            acquiredClueIds.Clear();
            acquiredClueIdSet.Clear();
            SelectedFinalChoiceId = string.Empty;
        }

        internal void Start(BranchRoute route)
        {
            Reset();
            CurrentRoute = route;
            Phase = Quarter3FlowPhase.CollectingClues;
        }

        internal bool AddClue(string clueId)
        {
            if (!acquiredClueIdSet.Add(clueId))
            {
                return false;
            }

            acquiredClueIds.Add(clueId);
            return true;
        }

        internal void OpenFinalChoice()
        {
            Phase = Quarter3FlowPhase.FinalChoiceOpen;
        }

        internal void Resolve(string finalChoiceId)
        {
            SelectedFinalChoiceId = finalChoiceId;
            Phase = Quarter3FlowPhase.Resolved;
        }

        public Quarter3SaveData CaptureState()
        {
            return new Quarter3SaveData
            {
                phase = (int)Phase,
                currentRoute = (int)CurrentRoute,
                acquiredClueIds = new List<string>(acquiredClueIds).ToArray(),
                selectedFinalChoiceId = SelectedFinalChoiceId
            };
        }

        public void Restore(Quarter3SaveData data)
        {
            Reset();
            if (data == null) return;

            Phase = (Quarter3FlowPhase)data.phase;
            CurrentRoute = (BranchRoute)data.currentRoute;
            if (data.acquiredClueIds != null)
            {
                foreach (string clueId in data.acquiredClueIds)
                {
                    if (!string.IsNullOrWhiteSpace(clueId) &&
                        acquiredClueIdSet.Add(clueId))
                        acquiredClueIds.Add(clueId);
                }
            }
            SelectedFinalChoiceId = data.selectedFinalChoiceId ?? string.Empty;
        }
    }
}
