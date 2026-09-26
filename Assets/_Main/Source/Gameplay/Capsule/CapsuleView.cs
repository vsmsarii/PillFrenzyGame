using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleView : MonoBehaviour
    {
        [SerializeField] private MaterialColorSetter m_ColorSetter;

        public void Initialize(Color color)
        {
            m_ColorSetter.SetColorIndex(color, 0);
        }
    }
}
