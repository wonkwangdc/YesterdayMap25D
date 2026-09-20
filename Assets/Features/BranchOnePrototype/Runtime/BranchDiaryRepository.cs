using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Diary
{
    public sealed class BranchDiaryRepository
    {
        private readonly List<DiaryDayRecord> records = new();
        private readonly ReadOnlyCollection<DiaryDayRecord> readOnlyRecords;

        public BranchDiaryRepository()
        {
            readOnlyRecords = records.AsReadOnly();
        }

        public DiaryDayRecord GetOrCreateRecord(int day, string mainDiary = "")
        {
            if (day < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "Day must be at least 1.");
            }

            DiaryDayRecord existing = records.Find(record => record.Day == day);
            if (existing != null)
            {
                return existing;
            }

            DiaryDayRecord created = new(day, mainDiary);
            records.Add(created);
            records.Sort((left, right) => left.Day.CompareTo(right.Day));
            return created;
        }

        public bool TryGetRecord(int day, out DiaryDayRecord record)
        {
            record = records.Find(candidate => candidate.Day == day);
            return record != null;
        }

        public void AddEventRecord(int day, string entry)
        {
            GetOrCreateRecord(day).AddEventRecord(entry);
        }

        public bool ReplaceLastEventRecord(int day, string entry)
        {
            return TryGetRecord(day, out DiaryDayRecord record) &&
                   record.ReplaceLastEventRecord(entry);
        }

        public void AddExplorationRecord(int day, string entry)
        {
            GetOrCreateRecord(day).AddExplorationRecord(entry);
        }

        public IReadOnlyList<DiaryDayRecord> GetRecords()
        {
            return readOnlyRecords;
        }

        public void Reset()
        {
            records.Clear();
        }

        public DiaryDaySaveData[] CaptureState()
        {
            DiaryDaySaveData[] result = new DiaryDaySaveData[records.Count];
            for (int i = 0; i < records.Count; i++)
            {
                DiaryDayRecord record = records[i];
                result[i] = new DiaryDaySaveData
                {
                    day = record.Day,
                    mainDiary = record.MainDiary,
                    eventRecords = new List<string>(record.EventRecords).ToArray(),
                    explorationRecords = new List<string>(record.ExplorationRecords).ToArray()
                };
            }
            return result;
        }

        public void RestoreState(DiaryDaySaveData[] data)
        {
            records.Clear();
            if (data == null) return;

            foreach (DiaryDaySaveData saved in data)
            {
                if (saved == null || saved.day < 1) continue;
                DiaryDayRecord record = new(saved.day, saved.mainDiary);
                record.RestoreRecords(saved.eventRecords, saved.explorationRecords);
                records.Add(record);
            }
            records.Sort((left, right) => left.Day.CompareTo(right.Day));
        }
    }
}
