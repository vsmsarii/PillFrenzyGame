using System;
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
        public int MaxHearts => m_MaxHearts < 0 ? 0 : m_MaxHearts;
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
            LevelRecordData[] scores = m_Data.LevelScores;
            if (scores == null)
                return 0;

            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i].LevelIndex == levelIndex)
                    return scores[i].Score;
            }

            return 0;
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
            m_MaxHearts = maxHeartCount < 0 ? 0 : maxHeartCount;
            m_HeartRefillMinutes = refillMinutes < 0f ? 0f : refillMinutes;

            if (!m_Data.HeartsInitialized)
            {
                m_Data.Hearts = m_MaxHearts;
                m_Data.HeartsInitialized = true;
                m_Data.NextHeartUnixUtc = 0;
                MarkDirty();
            }

            RefreshHearts();
        }

        public void RefreshHearts()
        {
            if (m_Data == null || !m_Data.HeartsInitialized)
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
            SpecialPowerSaveEntry[] powers = m_Data.SpecialPowers;
            if (powers == null)
                return null;

            int powerId = (int)id;
            for (int i = 0; i < powers.Length; i++)
            {
                if (powers[i].PowerId == powerId)
                    return powers[i];
            }

            return null;
        }

        private SpecialPowerSaveEntry FindOrCreatePower(ESpecialPowerId id)
        {
            SpecialPowerSaveEntry existing = FindPower(id);
            if (existing != null)
                return existing;

            SpecialPowerSaveEntry created = new SpecialPowerSaveEntry
            {
                PowerId = (int)id,
                Charges = 0,
                InitialGranted = false
            };

            SpecialPowerSaveEntry[] powers = m_Data.SpecialPowers;
            if (powers == null || powers.Length == 0)
            {
                m_Data.SpecialPowers = new[] { created };
                return created;
            }

            SpecialPowerSaveEntry[] expanded = new SpecialPowerSaveEntry[powers.Length + 1];
            Array.Copy(powers, expanded, powers.Length);
            expanded[powers.Length] = created;
            m_Data.SpecialPowers = expanded;
            return created;
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
            LevelRecordData[] scores = m_Data.LevelScores;
            if (scores == null)
                return null;

            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i].LevelIndex == levelIndex)
                    return scores[i];
            }

            return null;
        }

        private LevelRecordData FindOrCreateLevelRecord(int levelIndex)
        {
            LevelRecordData existing = FindLevelRecord(levelIndex);
            if (existing != null)
                return existing;

            LevelRecordData created = new LevelRecordData
            {
                LevelIndex = levelIndex,
                Score = 0,
                Attempts = 0,
                CompletionSeconds = 0
            };

            LevelRecordData[] scores = m_Data.LevelScores;
            if (scores == null || scores.Length == 0)
            {
                m_Data.LevelScores = new[] { created };
                return created;
            }

            LevelRecordData[] expanded = new LevelRecordData[scores.Length + 1];
            Array.Copy(scores, expanded, scores.Length);
            expanded[scores.Length] = created;
            m_Data.LevelScores = expanded;
            return created;
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
            if (data.LevelScores == null)
                data.LevelScores = Array.Empty<LevelRecordData>();

            if (data.SpecialPowers == null)
                data.SpecialPowers = Array.Empty<SpecialPowerSaveEntry>();

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
                CurrentLevelIndex = FirstLevelIndex,
                FirstLevelCompleted = false,
                LevelScores = Array.Empty<LevelRecordData>(),
                SpecialPowers = Array.Empty<SpecialPowerSaveEntry>()
            };
        }
    }
}
