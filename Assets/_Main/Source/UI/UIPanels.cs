using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.UI
{
    public static class UIPanels
    {
        public const int LoadingLayer = 2;

        public static async UniTask<GameObject> OpenAsync(
            EUIPanel panel,
            int layer = 0,
            bool additive = false,
            bool locked = false,
            CancellationToken cancellationToken = default)
        {
            UniTaskCompletionSource<GameObject> source = new UniTaskCompletionSource<GameObject>();

            void Unsubscribe()
            {
                EB.Presentation.Remove<UIPanelOpened>(OnOpened);
                EB.Presentation.Remove<UIPanelOpenFailed>(OnFailed);
            }

            void OnOpened(UIPanelOpened opened)
            {
                if (opened.Panel != panel)
                    return;

                Unsubscribe();
                source.TrySetResult(opened.Instance);
            }

            void OnFailed(UIPanelOpenFailed failed)
            {
                if (failed.Panel != panel)
                    return;

                Unsubscribe();
                source.TrySetResult(null);
            }

            EB.Presentation.Add<UIPanelOpened>(OnOpened);
            EB.Presentation.Add<UIPanelOpenFailed>(OnFailed);
            EB.Presentation.Invoke(new OpenUIPanelEvent(panel, layer, additive, locked));

            (bool canceled, GameObject instance) = await source.Task.AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();
            if (!canceled)
                return instance;

            Unsubscribe();
            return null;
        }

        public static void Close(EUIPanel panel)
        {
            EB.Presentation.Invoke(new CloseUIPanelEvent(panel));
        }

        public static async UniTask ShowLoading(CancellationToken cancellationToken = default)
        {
            await OpenAsync(EUIPanel.Loading, LoadingLayer, additive: false, locked: true, cancellationToken);
            SetLoadingProgress(0f);
        }

        public static void SetLoadingProgress(float progress)
        {
            EB.Presentation.Invoke(new LoadingProgressChanged(progress));
        }

        public static void HideLoading()
        {
            Close(EUIPanel.Loading);
        }
    }
}
