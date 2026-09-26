using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "CapsuleColor", menuName = "PillFrenzy/Capsule Color")]
    public sealed class CapsuleColorSO : ScriptableObject
    {
        [SerializeField] private Color m_Color = Color.white;

        public Color Color => m_Color;
    }
}
