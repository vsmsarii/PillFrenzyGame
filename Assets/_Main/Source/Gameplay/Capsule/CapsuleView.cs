using PillFrenzy.Material;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleView : MonoBehaviour
    {
        [SerializeField] private MaterialPropertySetter_Color m_ColorSetter;

        public void Initialize(Color color)
        {
            if (m_ColorSetter == null)
                m_ColorSetter = GetComponentInChildren<MaterialPropertySetter_Color>(true);

            if (m_ColorSetter != null)
                m_ColorSetter.SetColorIndex(color, 0);
        }
    }
}
