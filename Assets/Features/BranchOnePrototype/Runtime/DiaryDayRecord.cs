using System;
using System.Collections.Generic;

namespace YesterdayMap.BranchOne.Diary
{
    [Serializable]
    public sealed class DiaryDayRecord
    {
        private readonly List<string> eventRecords = new();
        private readonly List<string> explorationRecords = new();

        public int Day { get; }
        public string MainDiary { get; private set; }
        public IReadOnlyList<string> EventRecords => eventRecords;
        public IReadOnlyList<string> ExplorationRecords => explorationRecords;

        public DiaryDayRecord(int day, string mainDiary = "")
        {
            if (day < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "Day must be at least 1.");
            }

            Day = day;
            MainDiary = mainDiary ?? string.Empty;
        }

        public void SetMainDiary(string text)
        {
            MainDiary = text ?? string.Empty;
        }

        internal void AddEventRecord(string entry)
        {
            eventRecords.Add(entry ?? string.Empty);
        }

        internal bool ReplaceLastEventRecord(string entry)
        {
            if (eventRecords.Count == 0)
            {
                return false;
            }

            eventRecords[eventRecords.Count - 1] = entry ?? string.Empty;
            return true;
        }

        internal void AddExplorationRecord(string entry)
        {
            explorationRecords.Add(entry ?? string.Empty);
        }

        internal void RestoreRecords(
            IEnumerable<string> events,
            IEnumerable<string> explorations)
        {
            eventRecords.Clear();
            explorationRecords.Clear();
            if (events != null) eventRecords.AddRange(events);
            if (explorations != null) explorationRecords.AddRange(explorations);
        }
    }
}
