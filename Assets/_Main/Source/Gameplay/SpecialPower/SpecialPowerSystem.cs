using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class SpecialPowerSystem : ITickable
    {
        private readonly SpecialPowerCatalogSO m_Catalog;
        private readonly ISaveService m_Save;
        private readonly LevelSystem m_Level;

        private ESpecialPowerId m_ActiveId;
        private float m_ActiveRemaining;
        private float m_ActiveMultiplier = 1f;

        public SpecialPowerCatalogSO Catalog => m_Catalog;
        public bool IsAnyActive => m_ActiveId != ESpecialPowerId.None && m_ActiveRemaining > 0f;

        public SpecialPowerSystem(SpecialPowerCatalogSO catalog, ISaveService save, LevelSystem level)
        {
            m_Catalog = catalog;
            m_Save = save;
            m_Level = level;
        }

        public void SyncUnlockGrants()
        {
            SpecialPowerUnlockSync.Sync(m_Save, m_Catalog);
        }

        public bool IsUnlocked(ESpecialPowerId id)
        {
            if (m_Catalog == null || !m_Catalog.TryGet(id, out SpecialPowerCatalogEntry entry))
                return false;

            return m_Save.CurrentLevelNumber >= entry.UnlockLevel;
        }

        public int GetCharges(ESpecialPowerId id)
        {
            return m_Save != null ? m_Save.GetSpecialPowerCharges(id) : 0;
        }

        public bool TryActivate(ESpecialPowerId id)
        {
            if (m_Level == null || m_Level.Phase != ELevelPhase.Playing)
                return false;

            if (m_ActiveId != ESpecialPowerId.None && m_ActiveRemaining > 0f)
                return false;

            if (!IsUnlocked(id))
                return false;

            if (m_Catalog == null || !m_Catalog.TryGet(id, out SpecialPowerCatalogEntry entry))
                return false;

            SpecialPowerDefinitionSO definition = entry.Definition;
            if (definition == null)
                return false;

            if (!m_Save.TryConsumeSpecialPowerCharge(id))
                return false;

            m_ActiveId = id;
            m_ActiveRemaining = definition.Duration;
            m_ActiveMultiplier = definition.SpeedMultiplier;
            m_Level.SetSpeedMultiplier(m_ActiveMultiplier);
            EB.Analytics.Invoke(new SpecialPowerUseAnalytics(
                m_Level.LevelIndex,
                definition.Id,
                m_Level.Elapsed));
            PublishChanged();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (m_ActiveId == ESpecialPowerId.None || m_ActiveRemaining <= 0f)
                return;

            m_ActiveRemaining -= deltaTime;
            if (m_ActiveRemaining > 0f)
                return;

            ClearActive();
        }

        public void Shutdown()
        {
            ClearActive();
        }

        public float GetActiveRemaining(ESpecialPowerId id)
        {
            if (m_ActiveId != id)
                return 0f;

            return Mathf.Max(0f, m_ActiveRemaining);
        }

        public bool IsActive(ESpecialPowerId id)
        {
            return m_ActiveId == id && m_ActiveRemaining > 0f;
        }

        private void ClearActive()
        {
            m_ActiveId = ESpecialPowerId.None;
            m_ActiveRemaining = 0f;
            m_ActiveMultiplier = 1f;
            if (m_Level != null)
                m_Level.SetSpeedMultiplier(1f);

            PublishChanged();
        }

        private void PublishChanged()
        {
            EB.Presentation.Invoke(new SpecialPowerHudChanged());
        }
    }
}
