using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "CapsuleColor", menuName = "PillFrenzy/Capsule Color")]
    public sealed class CapsuleColorSO : ScriptableObject
    {
        [SerializeField] private Color m_Color = Color.white;

        private string m_CachedName;

        public Color Color => m_Color;
        public string DisplayName => m_CachedName ??= name;
    }
}
