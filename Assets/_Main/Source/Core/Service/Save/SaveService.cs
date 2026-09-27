using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PillFrenzy.Core
{
    public sealed class SaveService : Service, ISaveService, ILateTickable
    {
        private const int FirstLevelIndex = 0;
        private const int CurrentSaveVersion = 1;
        private const string SaveFileName = "save.json";
        private const float MaintenanceIntervalSeconds = 1f;

        private SaveData m_Data;
        private int m_MaxHearts;
        private float m_HeartRefillMinutes;
        private bool m_Dirty;
        private bool m_HeartsConfigured;
        private float m_NextMaintenanceTime;

        public static string FilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupPath => FilePath + ".bak";
        public static string TempPath => FilePath + ".tmp";

        public static void DeleteSaveFiles()
        {
            File.Delete(FilePath);
            File.Delete(BackupPath);
            File.Delete(TempPath);
        }

        public int CurrentLevelIndex => m_Data.CurrentLevelIndex;
        public int CurrentLevelNumber => m_Data.CurrentLevelIndex + 1;
        public bool HasCompletedFirstLevel => m_Data.FirstLevelCompleted;
        public int MaxHearts => m_MaxHearts;
        public int Hearts => m_Data.Hearts;

        public long SecondsUntilNextHeart
        {
            get
            {
                if (m_Data.Hearts >= m_MaxHearts || m_Data.NextHeartUnixUtc <= 0)
                    return 0;

                return Math.Max(0, m_Data.NextHeartUnixUtc - NowUnix);
            }
        }

        public long ImmortalRemainingSeconds => Math.Max(0, m_Data.ImmortalUntilUnixUtc - NowUnix);
        public bool IsImmortalActive => ImmortalRemainingSeconds > 0;

        private static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private long HeartRefillSeconds => (long)Math.Ceiling(m_HeartRefillMinutes * 60d);

        public int GetTotalScore() => m_Data.TotalScore;

        public int GetLevelAttempts(int levelIndex)
        {
            LevelRecordData record = FindLevelRecord(levelIndex);
            return record != null ? record.Attempts : 0;
        }

        public int IncrementLevelAttempts(int levelIndex)
        {
            LevelRecordData record = FindOrCreateLevelRecord(levelIndex);
            record.Attempts++;
            m_Data.TotalAttempts++;
            MarkDirty();
            return record.Attempts;
        }

        public void CompleteLevel(int levelIndex, int score, int completionSeconds)
        {
            LevelRecordData record = FindOrCreateLevelRecord(levelIndex);
            record.Score = Math.Max(record.Score, score);
            record.CompletionSeconds = Math.Max(record.CompletionSeconds, completionSeconds);

            if (levelIndex == FirstLevelIndex)
                m_Data.FirstLevelCompleted = true;

            if (levelIndex >= m_Data.CurrentLevelIndex)
                m_Data.CurrentLevelIndex = levelIndex + 1;

            m_Data.TotalCompletionSeconds += completionSeconds;
            m_Data.TotalScore += score;
            MarkDirty();
        }

        public int GetSpecialPowerCharges(ESpecialPowerId id)
        {
            SpecialPowerSaveEntry entry = FindPower(id);
            return entry != null ? entry.Charges : 0;
        }

        public bool TryConsumeSpecialPowerCharge(ESpecialPowerId id)
        {
            SpecialPowerSaveEntry entry = FindPower(id);
            if (entry == null || entry.Charges <= 0)
                return false;

            entry.Charges--;
            MarkDirty();
            return true;
        }

        public void AddSpecialPowerCharges(ESpecialPowerId id, int amount)
        {
            FindOrCreatePower(id).Charges += amount;
            MarkDirty();
        }

        public bool TryGrantInitialSpecialPower(ESpecialPowerId id, int charges)
        {
            SpecialPowerSaveEntry entry = FindOrCreatePower(id);
            if (entry.InitialGranted)
                return false;

            entry.InitialGranted = true;
            entry.Charges += charges;
            MarkDirty();
            return true;
        }

        public bool HasSeenTutorial(string tutorialId)
        {
            return m_Data.SeenTutorials.Contains(tutorialId);
        }

        public void MarkTutorialSeen(string tutorialId)
        {
            if (m_Data.SeenTutorials.Contains(tutorialId))
                return;

            m_Data.SeenTutorials.Add(tutorialId);
            MarkDirty();
        }

        public void GrantImmortalityMinutes(int minutes)
        {
            long start = Math.Max(m_Data.ImmortalUntilUnixUtc, NowUnix);
            m_Data.ImmortalUntilUnixUtc = start + minutes * 60L;
            MarkDirty();
        }

        public void ConfigureHearts(int maxHeartCount, float refillMinutes)
        {
            m_MaxHearts = maxHeartCount;
            m_HeartRefillMinutes = refillMinutes;
            m_HeartsConfigured = true;

            if (!m_Data.HeartsInitialized)
            {
                m_Data.Hearts = m_MaxHearts;
                m_Data.HeartsInitialized = true;
                m_Data.NextHeartUnixUtc = 0;
                MarkDirty();
            }

            if (m_Data.Hearts < m_MaxHearts && m_Data.NextHeartUnixUtc <= 0)
                ScheduleNextHeart();

            RefreshHearts();
        }

        public void RefreshHearts()
        {
            if (!m_HeartsConfigured)
                return;

            if (m_Data.Hearts >= m_MaxHearts)
            {
                if (m_Data.NextHeartUnixUtc != 0)
                {
                    m_Data.NextHeartUnixUtc = 0;
                    MarkDirty();
                }

                return;
            }

            if (m_Data.NextHeartUnixUtc <= 0)
                return;

            long now = NowUnix;
            while (m_Data.Hearts < m_MaxHearts && now >= m_Data.NextHeartUnixUtc)
            {
                m_Data.Hearts++;
                m_Data.NextHeartUnixUtc = m_Data.Hearts < m_MaxHearts ? m_Data.NextHeartUnixUtc + HeartRefillSeconds : 0;
                MarkDirty();
            }
        }

        public bool TrySpendHeart()
        {
            RefreshHearts();
            if (m_Data.Hearts <= 0)
                return false;

            m_Data.Hearts--;
            if (m_Data.Hearts < m_MaxHearts && m_Data.NextHeartUnixUtc <= 0)
                ScheduleNextHeart();

            MarkDirty();
            return true;
        }

        public void GrantHearts(int amount)
        {
            RefreshHearts();
            m_Data.Hearts += amount;
            if (m_Data.Hearts >= m_MaxHearts)
                m_Data.NextHeartUnixUtc = 0;

            MarkDirty();
        }

        public void FlushPending()
        {
            PersistIfDirty();
        }

        public void LateTick(float deltaTime)
        {
            if (Time.unscaledTime < m_NextMaintenanceTime)
                return;

            m_NextMaintenanceTime = Time.unscaledTime + MaintenanceIntervalSeconds;
            RefreshHearts();
            PersistIfDirty();
        }

        protected override void OnInitialize()
        {
            m_Data = Load();
        }

        protected override void OnDispose()
        {
            PersistIfDirty();
        }

        private void ScheduleNextHeart()
        {
            if (m_HeartRefillMinutes <= 0f)
                return;

            m_Data.NextHeartUnixUtc = NowUnix + HeartRefillSeconds;
            MarkDirty();
        }

        private SpecialPowerSaveEntry FindPower(ESpecialPowerId id)
        {
            List<SpecialPowerSaveEntry> powers = m_Data.SpecialPowers;
            for (int i = 0; i < powers.Count; i++)
            {
                if (powers[i].PowerId == (int)id)
                    return powers[i];
            }

            return null;
        }

        private SpecialPowerSaveEntry FindOrCreatePower(ESpecialPowerId id)
        {
            SpecialPowerSaveEntry entry = FindPower(id);
            if (entry != null)
                return entry;

            entry = new SpecialPowerSaveEntry { PowerId = (int)id };
            m_Data.SpecialPowers.Add(entry);
            return entry;
        }

        private LevelRecordData FindLevelRecord(int levelIndex)
        {
            List<LevelRecordData> records = m_Data.LevelScores;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].LevelIndex == levelIndex)
                    return records[i];
            }

            return null;
        }

        private LevelRecordData FindOrCreateLevelRecord(int levelIndex)
        {
            LevelRecordData record = FindLevelRecord(levelIndex);
            if (record != null)
                return record;

            record = new LevelRecordData { LevelIndex = levelIndex };
            m_Data.LevelScores.Add(record);
            return record;
        }

        private void MarkDirty()
        {
            m_Dirty = true;
        }

        private static SaveData Load()
        {
            SaveData data = Read(FilePath);
            if (data == null)
            {
                data = Read(BackupPath);
                if (data != null)
                    Logger.Warning("Save file unreadable, recovered from backup.");
            }

            if (data == null)
                return new SaveData { Version = CurrentSaveVersion, CurrentLevelIndex = FirstLevelIndex };

            data.LevelScores ??= new List<LevelRecordData>();
            data.SpecialPowers ??= new List<SpecialPowerSaveEntry>();
            data.SeenTutorials ??= new List<string>();
            return data;
        }

        private static SaveData Read(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Logger.Error("Save read failed at " + path + ": " + exception.Message);
                return null;
            }
        }

        private void PersistIfDirty()
        {
            if (!m_Dirty)
                return;

            try
            {
                File.WriteAllText(TempPath, JsonUtility.ToJson(m_Data));
                if (File.Exists(FilePath))
                    File.Replace(TempPath, FilePath, BackupPath);
                else
                    File.Move(TempPath, FilePath);

                m_Dirty = false;
            }
            catch (Exception exception)
            {
                Logger.Error("Save write failed: " + exception.Message);
            }
        }
    }
}
