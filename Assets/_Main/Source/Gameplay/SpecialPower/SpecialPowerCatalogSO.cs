using System;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct SpecialPowerCatalogEntry
    {
        [SerializeField] private SpecialPowerDefinitionSO m_Definition;
        [SerializeField] private int m_UnlockLevel;
        [SerializeField] private int m_InitialCharges;

        public SpecialPowerDefinitionSO Definition => m_Definition;
        public int UnlockLevel => m_UnlockLevel < 1 ? 1 : m_UnlockLevel;
        public int InitialCharges => m_InitialCharges < 0 ? 0 : m_InitialCharges;
    }

    [CreateAssetMenu(fileName = "SpecialPowerCatalog", menuName = "PillFrenzy/Special Power Catalog")]
    public sealed class SpecialPowerCatalogSO : ScriptableObject
    {
        [SerializeField] private SpecialPowerCatalogEntry[] m_Entries;

        public SpecialPowerCatalogEntry[] Entries => m_Entries;

        public bool TryGet(ESpecialPowerId id, out SpecialPowerCatalogEntry entry)
        {
            if (m_Entries == null)
            {
                entry = default;
                return false;
            }

            for (int i = 0; i < m_Entries.Length; i++)
            {
                SpecialPowerDefinitionSO definition = m_Entries[i].Definition;
                if (definition == null || definition.Id != id)
                    continue;

                entry = m_Entries[i];
                return true;
            }

            entry = default;
            return false;
        }

        public bool HasAnyUnlocked(int currentLevelNumber)
        {
            if (m_Entries == null)
                return false;

            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].Definition == null)
                    continue;

                if (currentLevelNumber >= m_Entries[i].UnlockLevel)
                    return true;
            }

            return false;
        }
    }
}
