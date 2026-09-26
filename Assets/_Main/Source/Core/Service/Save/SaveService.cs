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

        private readonly string m_Path;
        private readonly string m_TempPath;
        private readonly string m_BackupPath;
        private SaveData m_Data;
        private int m_MaxHearts;
        private float m_HeartRefillMinutes;
        private bool m_Dirty;
        private bool m_HeartsConfigured;

        public static string FilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupPath => FilePath + ".bak";
        public static string TempPath => FilePath + ".tmp";

        public static void DeleteSaveFiles()
        {
            TryDeleteFile(FilePath);
            TryDeleteFile(BackupPath);
            TryDeleteFile(TempPath);
        }

        private static void TryDeleteFile(string path)
        {
            if (!File.Exists(path))
                return;

            File.Delete(path);
        }

        public SaveService()
        {
            m_Path = FilePath;
            m_TempPath = TempPath;
            m_BackupPath = BackupPath;
        }

        public int CurrentLevelIndex => m_Data.CurrentLevelIndex;
        public int CurrentLevelNumber => m_Data.CurrentLevelIndex + 1;
        public bool HasCompletedFirstLevel => m_Data.FirstLevelCompleted;
        public int MaxHearts => m_MaxHearts;
        public int Hearts => m_Data.Hearts < 0 ? 0 : m_Data.Hearts;

        public long SecondsUntilNextHeart
        {
            get
            {
                if (m_Data.Hearts >= m_MaxHearts || m_Data.NextHeartUnixUtc <= 0)
                    return 0;

                long remaining = m_Data.NextHeartUnixUtc - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return remaining > 0 ? remaining : 0;
            }
        }

        public long ImmortalRemainingSeconds
        {
            get
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remaining = m_Data.ImmortalUntilUnixUtc - now;
                return remaining > 0 ? remaining : 0;
            }
        }

        public bool IsImmortalActive => ImmortalRemainingSeconds > 0;

        public int GetLevelScore(int levelIndex)
        {
            LevelRecordData record = FindLevelRecord(levelIndex);
            return record != null ? record.Score : 0;
        }

        public int GetTotalScore() => m_Data.TotalScore;
        public int GetTotalAttempts() => m_Data.TotalAttempts;
        public int GetTotalCompletionSeconds() => m_Data.TotalCompletionSeconds;

        public int GetLevelAttempts(int levelIndex)
        {
            LevelRecordData record = FindLevelRecord(levelIndex);
            return record != null ? record.Attempts : 0;
        }

        public void CompleteLevel(int levelIndex, int score, int completionSeconds)
        {
            UpsertScore(levelIndex, score, completionSeconds);

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
            SpecialPowerSaveEntry entry = FindOrCreatePower(id);

            if (entry.Charges <= 0)
                return false;

            entry.Charges--;

            MarkDirty();

            return true;
        }

        public void AddSpecialPowerCharges(ESpecialPowerId id, int amount)
        {
            if (amount <= 0 || id == ESpecialPowerId.None)
                return;

            SpecialPowerSaveEntry entry = FindOrCreatePower(id);
            entry.Charges += amount;

            MarkDirty();
        }

        public bool TryGrantInitialSpecialPower(ESpecialPowerId id, int charges)
        {
            if (id == ESpecialPowerId.None)
                return false;

            SpecialPowerSaveEntry entry = FindOrCreatePower(id);
            if (entry.InitialGranted)
                return false;

            entry.InitialGranted = true;
            if (charges > 0)
                entry.Charges += charges;
            MarkDirty();
            return true;
        }

        public void GrantImmortalityMinutes(int minutes)
        {
            if (minutes <= 0)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long current = m_Data.ImmortalUntilUnixUtc;
            long start = current > now ? current : now;
            m_Data.ImmortalUntilUnixUtc = start + minutes * 60L;
            MarkDirty();
        }

        public void ConfigureHearts(int maxHeartCount, float refillMinutes)
        {
            m_MaxHearts = Math.Max(0, maxHeartCount);
            m_HeartRefillMinutes = Math.Max(0f, refillMinutes);
            m_HeartsConfigured = true;

            if (!m_Data.HeartsInitialized)
            {
                m_Data.Hearts = m_MaxHearts;
                m_Data.HeartsInitialized = true;
                m_Data.NextHeartUnixUtc = 0;
                MarkDirty();
            }

            if (m_Data.Hearts < m_MaxHearts && m_Data.NextHeartUnixUtc <= 0 && HeartRefillSeconds > 0)
            {
                m_Data.NextHeartUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + HeartRefillSeconds;
                MarkDirty();
            }

            RefreshHearts();
        }

        public void RefreshHearts()
        {
            if (!m_HeartsConfigured || m_Data == null || !m_Data.HeartsInitialized)
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

            if (m_HeartRefillMinutes <= 0f || m_Data.NextHeartUnixUtc <= 0)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long refillSeconds = HeartRefillSeconds;
            if (refillSeconds <= 0)
                return;

            bool dirty = false;
            while (m_Data.Hearts < m_MaxHearts && now >= m_Data.NextHeartUnixUtc)
            {
                m_Data.Hearts++;
                dirty = true;
                if (m_Data.Hearts >= m_MaxHearts)
                {
                    m_Data.NextHeartUnixUtc = 0;
                    break;
                }

                m_Data.NextHeartUnixUtc += refillSeconds;
            }

            if (dirty)
                MarkDirty();
        }

        public bool TrySpendHeart()
        {
            RefreshHearts();
            if (m_Data.Hearts <= 0)
                return false;

            m_Data.Hearts--;
            if (m_Data.Hearts < m_MaxHearts && m_Data.NextHeartUnixUtc <= 0)
            {
                long refillSeconds = HeartRefillSeconds;
                m_Data.NextHeartUnixUtc = refillSeconds > 0
                    ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() + refillSeconds
                    : 0;
            }

            MarkDirty();
            return true;
        }

        public void GrantHearts(int amount)
        {
            if (amount <= 0)
                return;

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
            RefreshHearts();
            PersistIfDirty();
        }

        private long HeartRefillSeconds
        {
            get
            {
                if (m_HeartRefillMinutes <= 0f)
                    return 0;

                return (long)Math.Ceiling(m_HeartRefillMinutes * 60d);
            }
        }

        public int IncrementLevelAttempts(int levelIndex)
        {
            LevelRecordData record = FindOrCreateLevelRecord(levelIndex);
            record.Attempts++;
            m_Data.TotalAttempts++;
            MarkDirty();
            return record.Attempts;
        }

        protected override void OnInitialize()
        {
            m_Data = Load();
            Application.quitting += OnApplicationQuitting;
            Application.focusChanged += OnApplicationFocusChanged;
        }

        protected override void OnDispose()
        {
            Application.quitting -= OnApplicationQuitting;
            Application.focusChanged -= OnApplicationFocusChanged;
            PersistIfDirty();
        }

        private void OnApplicationQuitting()
        {
            PersistIfDirty();
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                PersistIfDirty();
        }

        private SpecialPowerSaveEntry FindPower(ESpecialPowerId id)
        {
            return m_Data.SpecialPowers.Find(entry => entry.PowerId == (int)id);
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

        private void UpsertScore(int levelIndex, int score, int completionSeconds)
        {
            LevelRecordData record = FindOrCreateLevelRecord(levelIndex);
            if (score > record.Score)
                record.Score = score;
            if (completionSeconds > record.CompletionSeconds)
                record.CompletionSeconds = completionSeconds;
        }

        private LevelRecordData FindLevelRecord(int levelIndex)
        {
            return m_Data.LevelScores.Find(record => record.LevelIndex == levelIndex);
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

        private SaveData Load()
        {
            SaveData data = TryRead(m_Path);
            if (data == null)
            {
                data = TryRead(m_BackupPath);
                if (data != null)
                    Logger.Warning("Save file unreadable, recovered from backup.");
            }

            return data != null ? Migrate(data) : CreateDefault();
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                Logger.Error("Save read failed at " + path + ": " + exception.Message);
                return null;
            }
        }

        private static SaveData Migrate(SaveData data)
        {
            data.LevelScores ??= new List<LevelRecordData>();
            data.SpecialPowers ??= new List<SpecialPowerSaveEntry>();

            if (data.Version < CurrentSaveVersion)
                data.Version = CurrentSaveVersion;

            return data;
        }

        private void MarkDirty()
        {
            m_Dirty = true;
        }

        private void PersistIfDirty()
        {
            if (!m_Dirty || m_Data == null)
                return;

            try
            {
                File.WriteAllText(m_TempPath, JsonUtility.ToJson(m_Data));
                if (File.Exists(m_Path))
                    File.Replace(m_TempPath, m_Path, m_BackupPath);
                else
                    File.Move(m_TempPath, m_Path);

                m_Dirty = false;
            }
            catch (Exception exception)
            {
                Logger.Error("Save write failed: " + exception.Message);
            }
        }

        private static SaveData CreateDefault()
        {
            return new SaveData
            {
                Version = CurrentSaveVersion,
                CurrentLevelIndex = FirstLevelIndex
            };
        }
    }
}
