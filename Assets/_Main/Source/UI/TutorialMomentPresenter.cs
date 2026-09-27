using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Gameplay;
using UnityEngine;

namespace PillFrenzy.UI
{
    public sealed class TutorialMomentPresenter : ITutorialMomentPresenter
    {
        private const int Layer = 1;

        private readonly CancellationToken m_Token;
        private TutorialOverlayUI m_Overlay;
        private TutorialMomentView m_View;
        private bool m_HasView;
        private bool m_Opening;

        public TutorialMomentPresenter(CancellationToken token)
        {
            m_Token = token;
        }

        public void Show(TutorialMomentView view)
        {
            m_View = view;
            m_HasView = true;

            if (m_Overlay != null && m_Overlay.isActiveAndEnabled)
            {
                m_Overlay.Show(view);
                return;
            }

            if (!m_Opening)
                OpenAsync().Forget();
        }

        public void Hide()
        {
            m_HasView = false;
            if (m_Overlay == null)
                return;

            m_Overlay = null;
            UIPanels.Close(EUIPanel.TutorialOverlay);
        }

        private async UniTaskVoid OpenAsync()
        {
            m_Opening = true;
            GameObject instance = await UIPanels.OpenAsync(EUIPanel.TutorialOverlay, Layer, additive: true, cancellationToken: m_Token);
            m_Opening = false;

            if (m_Token.IsCancellationRequested || instance == null)
                return;

            if (!m_HasView)
            {
                UIPanels.Close(EUIPanel.TutorialOverlay);
                return;
            }

            m_Overlay = instance.GetComponent<TutorialOverlayUI>();
            m_Overlay.Show(m_View);
        }
    }
}
