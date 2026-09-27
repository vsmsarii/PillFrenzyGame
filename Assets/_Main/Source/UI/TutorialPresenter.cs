using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Gameplay;
using UnityEngine;

namespace PillFrenzy.UI
{
    public sealed class TutorialPresenter : ITutorialPresenter
    {
        private const int Layer = 1;

        public async UniTask<TutorialPresentResult> PresentAsync(
            TutorialDefinitionSO tutorial,
            Action<int> onPageShown,
            CancellationToken cancellationToken)
        {
            GameObject instance = await UIPanels.OpenAsync(EUIPanel.Tutorial, Layer, additive: true, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (instance == null)
                return new TutorialPresentResult(ETutorialOutcome.Unavailable, 0);

            TutorialCanvasUI canvas = instance.GetComponent<TutorialCanvasUI>();

            try
            {
                return await canvas.Play(tutorial, onPageShown).AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                UIPanels.Close(EUIPanel.Tutorial);
            }
        }
    }
}
