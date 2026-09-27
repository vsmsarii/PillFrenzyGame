using System.Collections.Generic;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class SpecialPowerSystem : ITickable
    {
        private readonly SpecialPowerCatalogSO m_Catalog;
        private readonly ISaveService m_Save;
        private readonly ILevelRunState m_Level;
        private readonly SpawnPacingSystem m_Pacing;
        private readonly HashSet<ESpecialPowerId> m_Concealed = new();

        private ESpecialPowerId m_ActiveId;
        private float m_ActiveRemaining;

        public SpecialPowerCatalogSO Catalog => m_Catalog;
        public bool IsAnyActive => m_ActiveId != ESpecialPowerId.None;

        private int ReachedLevelNumber => Mathf.Max(m_Save.CurrentLevelNumber, m_Level.LevelIndex + 1);

        public bool HasAnyRevealed
        {
            get
            {
                foreach (SpecialPowerCatalogEntry entry in m_Catalog.Entries)
                {
                    if (entry.Definition != null && IsRevealed(entry.Definition.Id))
                        return true;
                }

                return false;
            }
        }

        public SpecialPowerSystem(SpecialPowerCatalogSO catalog, ISaveService save, ILevelRunState level, SpawnPacingSystem pacing)
        {
            m_Catalog = catalog;
            m_Save = save;
            m_Level = level;
            m_Pacing = pacing;

            EB.Gameplay.Add<TutorialFinished>(OnTutorialFinished);
        }

        public void SyncUnlockGrants()
        {
            SpecialPowerUnlockSync.Sync(m_Save, m_Catalog, ReachedLevelNumber);
        }

        public bool IsUnlocked(ESpecialPowerId id)
        {
            return m_Catalog.TryGet(id, out SpecialPowerCatalogEntry entry) && ReachedLevelNumber >= entry.UnlockLevel;
        }

        public bool IsRevealed(ESpecialPowerId id)
        {
            return IsUnlocked(id) && !m_Concealed.Contains(id);
        }

        public bool IsActive(ESpecialPowerId id)
        {
            return m_ActiveId == id;
        }

        public int GetCharges(ESpecialPowerId id)
        {
            return m_Save.GetSpecialPowerCharges(id);
        }

        public float GetActiveRemaining(ESpecialPowerId id)
        {
            return IsActive(id) ? m_ActiveRemaining : 0f;
        }

        public void Conceal(ESpecialPowerId id)
        {
            if (m_Concealed.Add(id))
                PublishChanged();
        }

        public void Reveal(ESpecialPowerId id)
        {
            if (m_Concealed.Remove(id))
                PublishChanged();
        }

        public bool TryActivate(ESpecialPowerId id)
        {
            if (!m_Level.IsSimulating || IsAnyActive || !IsRevealed(id))
                return false;

            if (!m_Catalog.TryGet(id, out SpecialPowerCatalogEntry entry) || !m_Save.TryConsumeSpecialPowerCharge(id))
                return false;

            m_ActiveId = id;
            m_ActiveRemaining = entry.Definition.Duration;
            m_Pacing.SetSpeedMultiplier(entry.Definition.SpeedMultiplier);
            EB.Analytics.Invoke(new SpecialPowerUseAnalytics(m_Level.LevelIndex, id, m_Level.Elapsed));
            PublishChanged();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsAnyActive || m_Level.IsPaused)
                return;

            m_ActiveRemaining = Mathf.Max(0f, m_ActiveRemaining - deltaTime);
            if (m_ActiveRemaining <= 0f)
                ClearActive();
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<TutorialFinished>(OnTutorialFinished);
            ClearActive();
        }

        private void OnTutorialFinished(TutorialFinished evt)
        {
            if (evt.Tutorial.RevealsSpecialPower)
                Reveal(evt.Tutorial.SpecialPower);
        }

        private void ClearActive()
        {
            m_ActiveId = ESpecialPowerId.None;
            m_ActiveRemaining = 0f;
            m_Pacing.ClearSpeedMultiplier();
            PublishChanged();
        }

        private void PublishChanged()
        {
            EB.Presentation.Invoke(new SpecialPowerHudChanged());
        }
    }
}
