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
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            GameLoop loop = new GameLoop();
            ServiceProvider services = new ServiceProvider(loop);
            GameContext context = new GameContext(loop, services);

            GameRunner runner = host.GetComponent<GameRunner>();
            if (runner == null)
                runner = host.AddComponent<GameRunner>();

            runner.Bind(context);

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
            services.Register<IAnalyticsSystem>(analytics);

            return context;
        }

        public static async UniTask LoadGlobalsAsync(GameContext context)
        {
            CancellationToken token = context.CancellationToken;
            IAssetProvider assets = context.Services.Get<IAssetProvider>();
            ISaveService save = context.Services.Get<ISaveService>();

            await assets.InitializeAsync(token);
            await context.Services.Get<IInputService>().InitializeAsync(token);
            await context.Services.Get<IAudioService>().InitializeAsync(token);
            await context.Services.Get<IIAPService>().InitializeAsync(token);

            GlobalSettingsSO globalSettings = await assets.LoadAsset<GlobalSettingsSO>(AddressableKeys.GlobalSettings, token);
            if (globalSettings != null)
            {
                context.GlobalSettings = globalSettings;
                Application.targetFrameRate = globalSettings.TargetFrameRate;
                save.ConfigureHearts(globalSettings.DefaultHeartCount, globalSettings.HeartRefillMinutes);
            }
            else
            {
                Logger.Error("GlobalSettings asset missing.");
            }

            LevelManifestSO levelManifest = await assets.LoadAsset<LevelManifestSO>(AddressableKeys.LevelManifest, token);
            if (levelManifest != null)
                context.LevelCatalog = levelManifest;
            else
                Logger.Error("LevelManifest asset missing.");

            UIPanelCatalogSO catalog = await assets.LoadAsset<UIPanelCatalogSO>(AddressableKeys.UiPanelCatalog, token);
            UiEventSystem.Ensure();
            UIRoot uiRoot = Object.FindAnyObjectByType<UIRoot>();
            if (uiRoot != null)
                uiRoot.Initialize(assets, catalog);
            else
                Logger.Error("UIRoot missing in scene.");
        }
    }
}
