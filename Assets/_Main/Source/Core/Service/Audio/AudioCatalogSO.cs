using System;
using UnityEngine;

namespace PillFrenzy.Core
{
    [Serializable]
    public struct AudioCatalogEntry
    {
        [SerializeField] private EAudioName m_Name;
        [SerializeField] private AudioClip m_Clip;

        public EAudioName Name => m_Name;
        public AudioClip Clip => m_Clip;
    }

    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "PillFrenzy/Audio Catalog")]
    public sealed class AudioCatalogSO : ScriptableObject
    {
        [SerializeField] private AudioCatalogEntry[] m_Entries;

        public AudioCatalogEntry[] Entries => m_Entries;
    }
}
