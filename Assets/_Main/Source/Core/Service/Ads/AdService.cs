using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Core
{
    public sealed class AdService : Service, IAdService
    {
        private readonly IAssetProvider m_Assets;
        private readonly IAdProvider m_Provider;
        private AdSettingsSO m_Settings;

        public AdService(IAssetProvider assets, IAdProvider provider)
        {
            m_Assets = assets;
            m_Provider = provider;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            m_Settings = await m_Assets.LoadAsset<AdSettingsSO>(AddressableKeys.AdSettings, cancellationToken);
            await m_Provider.InitializeAsync(m_Settings.TestAds, cancellationToken);
        }

        public bool IsDueAfterLevel(int levelIndex)
        {
            int completedLevelNumber = levelIndex + 1;
            return completedLevelNumber % m_Settings.LevelInterval == 0;
        }

        public async UniTask<EAdResult> ShowAsync(int levelIndex, CancellationToken cancellationToken = default)
        {
            EAdType type = m_Settings.Type;
            EB.Presentation.Invoke(new AdStarted(type));

            EAdResult result = await m_Provider.ShowAsync(type, m_Settings.AdUnitId, cancellationToken);

            EB.Presentation.Invoke(new AdFinished(type, result));
            EB.Analytics.Invoke(new AdShowAnalytics(levelIndex, type, result));
            return result;
        }
    }
}
