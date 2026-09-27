using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct ForcedCapsuleSpawn
    {
        [SerializeField, Min(0)] private int m_SpawnIndex;
        [SerializeField] private ECapsuleKind m_Kind;

        public int SpawnIndex => m_SpawnIndex;
        public ECapsuleKind Kind => m_Kind;
    }
}
