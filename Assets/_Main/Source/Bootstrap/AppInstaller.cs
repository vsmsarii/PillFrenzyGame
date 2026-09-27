using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public static class AppInstaller
    {
        public static GameContext CreateContext(GameObject host)
        {
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            GameLoop loop = new GameLoop();
            ServiceProvider services = new ServiceProvider(loop);
            GameContext context = new GameContext(loop, services);
            host.AddComponent<GameRunner>().Bind(context);

            IAssetProvider assets = new AssetProvider();
            ISaveService save = new SaveService();
            AnalyticsSystem analytics = new AnalyticsSystem();
            analytics.Register(new AnalyticsLog());
            ISceneLoadingUi loadingUi = new SceneLoadingUi();

            services.Register<IAssetProvider>(assets);
            services.Register<IGameObjectPool>(new GameObjectPool(assets));
            services.Register<ISceneLoadingUi>(loadingUi);
            services.Register<ISceneService>(new SceneService(loadingUi));
            services.Register<IInputService>(new InputService(assets));
            services.Register<IAudioService>(new AudioSystem(assets));
            services.Register<ISaveService>(save);
            services.Register<IIAPService>(new IAPService(assets, save));
            services.Register<IAdService>(new AdService(assets, new DummyAdProvider(assets)));
            services.Register<IAnalyticsSystem>(analytics);

            return context;
        }

        public static async UniTask LoadGlobalsAsync(GameContext context)
        {
            CancellationToken token = context.CancellationToken;
            IAssetProvider assets = context.Services.Get<IAssetProvider>();

            await assets.InitializeAsync(token);
            await context.Services.Get<IInputService>().InitializeAsync(token);
            await context.Services.Get<IAudioService>().InitializeAsync(token);
            await context.Services.Get<IIAPService>().InitializeAsync(token);
            await context.Services.Get<IAdService>().InitializeAsync(token);

            GlobalSettingsSO globalSettings = await assets.LoadAsset<GlobalSettingsSO>(AddressableKeys.GlobalSettings, token);
            context.GlobalSettings = globalSettings;
            Application.targetFrameRate = globalSettings.TargetFrameRate;
            context.Services.Get<ISaveService>().ConfigureHearts(globalSettings.DefaultHeartCount, globalSettings.HeartRefillMinutes);

            context.LevelCatalog = await assets.LoadAsset<LevelManifestSO>(AddressableKeys.LevelManifest, token);

            UIPanelCatalogSO panelCatalog = await assets.LoadAsset<UIPanelCatalogSO>(AddressableKeys.UiPanelCatalog, token);
            UiEventSystem.Ensure();
            Object.FindAnyObjectByType<UIRoot>().Initialize(assets, panelCatalog);
        }
    }
}
