using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class BootInit : MonoBehaviour
    {
        private GameContext m_Context;

        private void Awake()
        {
            if (GameRunner.Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Logger.SetMinimumLevel(ELogLevel.Warning);
#endif

            GameLoop loop = new GameLoop();
            ServiceProvider services = new ServiceProvider(loop);
            m_Context = new GameContext(loop, services);

            GameRunner runner = gameObject.GetComponent<GameRunner>();
            if (runner == null)
                runner = gameObject.AddComponent<GameRunner>();

            runner.Bind(m_Context);

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
        }

        private void Start()
        {
            if (m_Context == null)
                return;

            BootAsync().Forget();
        }

        private async UniTaskVoid BootAsync()
        {
            IAssetProvider assets = m_Context.Services.Get<IAssetProvider>();
            IInputService input = m_Context.Services.Get<IInputService>();
            IAudioService audio = m_Context.Services.Get<IAudioService>();
            ISceneService scenes = m_Context.Services.Get<ISceneService>();
            ISaveService save = m_Context.Services.Get<ISaveService>();
            IIAPService iap = m_Context.Services.Get<IIAPService>();

            await assets.InitializeAsync(m_Context.CancellationToken);
            await input.InitializeAsync(m_Context.CancellationToken);
            await audio.InitializeAsync(m_Context.CancellationToken);
            await iap.InitializeAsync(m_Context.CancellationToken);

            GlobalSettingsSO globalSettings = await assets.LoadAsset<GlobalSettingsSO>(AddressableKeys.GlobalSettings, m_Context.CancellationToken);
            if (globalSettings != null)
            {
                m_Context.GlobalSettings = globalSettings;
                Application.targetFrameRate = globalSettings.TargetFrameRate;
                save.ConfigureHearts(globalSettings.DefaultHeartCount, globalSettings.HeartRefillMinutes);
            }
            else
            {
                Logger.Error("GlobalSettings asset missing.");
            }

            LevelManifestSO levelManifest = await assets.LoadAsset<LevelManifestSO>(AddressableKeys.LevelManifest, m_Context.CancellationToken);
            if (levelManifest != null)
                m_Context.LevelCatalog = levelManifest;
            else
                Logger.Error("LevelManifest asset missing.");

            UIPanelCatalogSO catalog = await assets.LoadAsset<UIPanelCatalogSO>(AddressableKeys.UiPanelCatalog, m_Context.CancellationToken);
            UiEventSystem.Ensure();
            UIRoot uiRoot = FindAnyObjectByType<UIRoot>();
            if (uiRoot != null)
                uiRoot.Initialize(assets, catalog);
            else
                Logger.Error("UIRoot missing in Init scene.");

            ESceneName scene = save.HasCompletedFirstLevel ? ESceneName.Menu : ESceneName.Gameplay;
            await scenes.Load(scene, m_Context.CancellationToken);
        }
    }
}
