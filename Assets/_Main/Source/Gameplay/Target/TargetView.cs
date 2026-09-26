using DG.Tweening;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class TargetView : MonoBehaviour
    {
        [SerializeField] private MaterialColorSetter m_ColorSetter;

        public void Initialize(Color color)
        {
            m_ColorSetter.SetColorIndex(color, 1);
        }

        public void PlayLanded()
        {
            transform.DOPunchScale(Vector3.one * 0.08f, 0.18f, 6, 0.6f).SetLink(gameObject);
        }
    }
}
