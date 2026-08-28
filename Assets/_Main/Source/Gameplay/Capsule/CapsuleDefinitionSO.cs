using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(menuName = "PillFrenzy/Capsule Definition", fileName = "CapsuleDefinition")]
    public sealed class CapsuleDefinitionSO : ScriptableObject
    {
        [SerializeField] private ECapsuleKind m_Kind = ECapsuleKind.Normal;
        [SerializeField] private ECapsuleColor m_Color = ECapsuleColor.Red;
        [SerializeField] private string m_PrefabKey = AddressableKeys.CapsulePrefab;
        [SerializeField] private float m_FlyDuration = 0.35f;

        public ECapsuleKind Kind => m_Kind;
        public ECapsuleColor Color => m_Color;
        public string PrefabKey => m_PrefabKey;
        public float FlyDuration => m_FlyDuration;
    }
}
