using System.Collections.Generic;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "VfxCatalog", menuName = "PillFrenzy/VFX Catalog")]
    public sealed class VfxCatalogSO : ScriptableObject
    {
        [SerializeField] private List<VfxEntry> m_Entries = new();

        public IReadOnlyList<VfxEntry> Entries => m_Entries;
    }
}
