using PillFrenzy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class LoadingCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Percent;
        [SerializeField] private Image m_Fill;

        private void OnEnable()
        {
            EB.Presentation.Add<LoadingProgressChanged>(OnProgressChanged);
            Apply(0f);
        }

        private void OnDisable()
        {
            EB.Presentation.Remove<LoadingProgressChanged>(OnProgressChanged);
        }

        private void OnProgressChanged(LoadingProgressChanged evt)
        {
            Apply(evt.Progress);
        }

        private void Apply(float progress)
        {
            float clamped = Mathf.Clamp01(progress);
            m_Fill.fillAmount = clamped;

            UiText.Begin();
            UiText.Append(Mathf.RoundToInt(clamped * 100f));
            UiText.Append('%');
            UiText.ApplyTo(m_Percent);
        }
    }
}
