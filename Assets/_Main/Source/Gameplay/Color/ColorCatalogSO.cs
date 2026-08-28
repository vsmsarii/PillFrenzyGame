using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct ColorCatalogEntry
    {
        [SerializeField] private ECapsuleColor m_Id;
        [SerializeField] private Color m_Color;

        public ECapsuleColor Id => m_Id;
        public Color Color => m_Color;
    }

    [CreateAssetMenu(menuName = "PillFrenzy/Color Catalog", fileName = "ColorCatalog")]
    public sealed class ColorCatalogSO : ScriptableObject
    {
        [SerializeField] private ColorCatalogEntry[] m_Entries;

        public Color Get(ECapsuleColor id)
        {
            if (m_Entries == null)
                return Color.white;

            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].Id == id)
                    return m_Entries[i].Color;
            }

            return Color.white;
        }
    }
}
