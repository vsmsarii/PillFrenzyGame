using DG.Tweening;
using PillFrenzy.Material;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class TargetView : MonoBehaviour
    {
        [SerializeField] private MaterialPropertySetter_Color m_ColorSetter;

        public void Initialize(Color color)
        {
            if (m_ColorSetter == null)
                m_ColorSetter = GetComponentInChildren<MaterialPropertySetter_Color>(true);

            if (m_ColorSetter != null)
                m_ColorSetter.SetColorIndex(color, 1);
        }

        public void PlayLanded()
        {
            transform.DOPunchScale(Vector3.one * 0.08f, 0.18f, 6, 0.6f).SetLink(gameObject);
        }
    }
}
