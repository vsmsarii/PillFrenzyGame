using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.UI
{
    public sealed class DummyAdProvider : IAdProvider
    {
        private readonly IAssetProvider m_Assets;
        private bool m_TestAds;

        public DummyAdProvider(IAssetProvider assets)
        {
            m_Assets = assets;
        }

        public UniTask InitializeAsync(bool testAds, CancellationToken cancellationToken)
        {
            m_TestAds = testAds;
            return UniTask.CompletedTask;
        }

        public async UniTask<EAdResult> ShowAsync(EAdType type, string adUnitId, CancellationToken cancellationToken)
        {
            GameObject instance = await m_Assets.Instantiate(AddressableKeys.DummyAdView, null, cancellationToken);
            if (instance == null)
                return EAdResult.Failed;

            DummyAdView view = instance.GetComponent<DummyAdView>();
            bool canceled = await view.ShowAsync(type, adUnitId, m_TestAds, cancellationToken).SuppressCancellationThrow();
            m_Assets.ReleaseInstance(instance);
            return canceled ? EAdResult.Failed : EAdResult.Completed;
        }
    }
}
