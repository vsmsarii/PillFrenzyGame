using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class BootGameplay : MonoBehaviour
    {
        private GameContext m_Context;
        private IAssetProvider m_Assets;
        private CancellationTokenSource m_Cts;
        private GameObject m_LayoutInstance;
        private CapsuleSystem m_CapsuleSystem;
        private SpawnSystem m_SpawnSystem;
        private LevelSystem m_LevelSystem;
        private TargetSystem m_TargetSystem;
        private SpecialPowerSystem m_PowerSystem;
        private GameplayFeedback m_Feedback;
        private int m_LevelIndex;
        private LevelDefinitionSO m_Definition;
        private bool m_ReturnToMenu;
        private RunEnded m_LastRunEnded;
        private bool m_HasRunEnded;
        private bool m_SettingsOpen;

        private void Awake()
        {
            if (GameRunner.Instance == null)
            {
                Logger.Error("GameRunner missing. Play from Init.");
                return;
            }

            m_Context = GameRunner.Instance.Context;
            m_Cts = new CancellationTokenSource();
        }

        private void Start()
        {
            if (m_Context == null)
                return;

            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            m_Assets = m_Context.Services.Get<IAssetProvider>();
            ISaveService save = m_Context.Services.Get<ISaveService>();

            m_LevelIndex = m_Context.GameplayLevelIndex >= 0 ? m_Context.GameplayLevelIndex : save.CurrentLevelIndex;
            if (m_Context.LevelCatalog != null && m_LevelIndex > m_Context.LevelCatalog.LastLevelIndex)
                m_LevelIndex = m_Context.LevelCatalog.LastLevelIndex;
            if (m_LevelIndex < 0)
                m_LevelIndex = 0;

            UIPanels.SetLoadingProgress(0.75f);
            ColorCatalogSO colorTable = await m_Assets.LoadAsset<ColorCatalogSO>(AddressableKeys.ColorTable, m_Cts.Token);
            if (m_Cts == null)
                return;

            UIPanels.SetLoadingProgress(0.8f);
            TargetCatalogSO targetCatalog = await m_Assets.LoadAsset<TargetCatalogSO>(AddressableKeys.TargetCatalog, m_Cts.Token);
            if (m_Cts == null)
                return;

            UIPanels.SetLoadingProgress(0.85f);
            if (m_Context.LevelCatalog == null || !m_Context.LevelCatalog.TryGetDefinitionKey(m_LevelIndex, out string definitionKey))
            {
                Logger.Error("Level definition key missing for index " + m_LevelIndex + ".");
                UIPanels.HideLoading();
                return;
            }

            LevelDefinitionSO definition = await m_Assets.LoadAsset<LevelDefinitionSO>(definitionKey, m_Cts.Token);
            if (m_Cts == null)
                return;

            if (definition == null)
            {
                Logger.Error("Level definition missing for index " + m_LevelIndex + ".");
                UIPanels.HideLoading();
                return;
            }

            UIPanels.SetLoadingProgress(0.9f);
            if (!definition.TryGetLayoutKey(out string layoutKey)
                && (m_Context.LevelCatalog == null || !m_Context.LevelCatalog.TryGetDefaultLayoutKey(out layoutKey)))
            {
                Logger.Error("Level layout missing for index " + m_LevelIndex + ".");
                UIPanels.HideLoading();
                return;
            }

            m_LayoutInstance = await m_Assets.Instantiate(layoutKey, null, m_Cts.Token);
            if (m_Cts == null || m_LayoutInstance == null)
                return;

            LevelLayout layout = m_LayoutInstance.GetComponent<LevelLayout>();
            if (layout == null || layout.Path == null)
            {
                Logger.Error("Level prefab is missing LevelLayout or path.");
                UIPanels.HideLoading();
                return;
            }

            Camera camera = layout.Camera != null ? layout.Camera : Camera.main;
            if (camera == null)
            {
                Logger.Error("No camera for tap raycast.");
                UIPanels.HideLoading();
                return;
            }

            IInputService input = m_Context.Services.Get<IInputService>();
            IAudioService audio = m_Context.Services.Get<IAudioService>();
            audio.PlayMusic(EAudioName.MusicGameplay);
            IGameObjectPool pool = m_Context.Services.Get<IGameObjectPool>();

            FeedbackSettingsSO feedbackSettings = await m_Assets.LoadAsset<FeedbackSettingsSO>(AddressableKeys.FeedbackSettings, m_Cts.Token);
            if (m_Cts == null)
                return;

            Transform shakeTarget = layout.ShakeHolder != null ? layout.ShakeHolder : camera.transform;
            m_Feedback = new GameplayFeedback(pool, audio, shakeTarget, feedbackSettings, m_Cts.Token);
            CapsuleFactory factory = new CapsuleFactory(pool, layout.CapsuleRoot);
            TargetFactory targetFactory = new TargetFactory(pool, targetCatalog);
            m_TargetSystem = new TargetSystem(colorTable, targetFactory);
            UIPanels.SetLoadingProgress(0.95f);
            await m_TargetSystem.Bind(definition, layout, m_Cts.Token);
            if (m_Cts == null)
                return;

            await pool.Warmup(AddressableKeys.CapsulePrefab, definition.MaxActive + 2, m_Cts.Token);
            if (m_Cts == null)
                return;

            m_CapsuleSystem = new CapsuleSystem(
                input,
                m_TargetSystem,
                camera,
                m_Cts.Token,
                colorTable,
                m_Feedback,
                layout.CapsuleMask);
            m_SpawnSystem = new SpawnSystem(factory, m_CapsuleSystem);
            m_LevelSystem = new LevelSystem(m_SpawnSystem, m_CapsuleSystem, layout.Path, save, m_LevelIndex, m_Cts.Token, m_Feedback);
            m_CapsuleSystem.Bind(m_SpawnSystem, m_LevelSystem);

            SpecialPowerCatalogSO powerCatalog = await m_Assets.LoadAsset<SpecialPowerCatalogSO>(AddressableKeys.SpecialPowerCatalog, m_Cts.Token);
            if (m_Cts == null)
                return;

            m_PowerSystem = new SpecialPowerSystem(powerCatalog, save, m_LevelSystem);
            m_PowerSystem.SyncUnlockGrants();
            m_Definition = definition;
            m_ReturnToMenu = definition.ReturnToMenu;

            EB.Presentation.Add<RunEnded>(OnRunEnded);
            EB.Presentation.Add<UIPanelOpened>(OnPanelOpened);
            EB.Presentation.Add<ApplicationPauseChanged>(OnApplicationPauseChanged);
            m_Context.GameLoop.Register(m_CapsuleSystem);
            m_Context.GameLoop.Register(m_LevelSystem);
            m_Context.GameLoop.Register(m_PowerSystem);
            UIPanels.SetLoadingProgress(1f);
            EB.Presentation.Invoke(new OpenUIPanelEvent(EUIPanel.Gameplay, 0));
        }

        private void OnRunEnded(RunEnded evt)
        {
            m_LastRunEnded = evt;
            m_HasRunEnded = true;
            EUIPanel panel = evt.IsComplete ? EUIPanel.Win : EUIPanel.Lose;
            EB.Presentation.Invoke(new OpenUIPanelEvent(panel, 1));
        }

        private void OnPanelOpened(UIPanelOpened opened)
        {
            if (opened.Instance == null)
                return;

            if (opened.Panel == EUIPanel.Gameplay)
            {
                m_TargetSystem.PublishFill();
                if (m_Definition != null)
                {
                    LevelDefinitionSO definition = m_Definition;
                    m_Definition = null;
                    m_LevelSystem.StartRun(definition);
                }

                GameplayCanvasUI gameplayUi = opened.Instance.GetComponent<GameplayCanvasUI>();
                if (gameplayUi != null)
                {
                    gameplayUi.BindLevel(m_LevelIndex + 1);
                    if (m_PowerSystem != null)
                        gameplayUi.BindPowers(m_PowerSystem, m_Context.Services.Get<ISaveService>());
                    gameplayUi.BindSettings(OpenSettings);
                }

                UIPanels.HideLoading();
                return;
            }

            if (opened.Panel == EUIPanel.Settings)
            {
                SettingsCanvasUI settings = opened.Instance.GetComponent<SettingsCanvasUI>();
                if (settings != null)
                {
                    settings.Bind(
                        m_Context.Services.Get<IAudioService>(),
                        CloseSettings,
                        () => QuitMatchToMenu().Forget());
                }

                return;
            }

            if (!m_HasRunEnded)
                return;

            if (opened.Panel == EUIPanel.Win && m_LastRunEnded.IsComplete)
            {
                WinCanvasUI win = opened.Instance.GetComponent<WinCanvasUI>();
                if (win != null)
                {
                    int lastLevelIndex = m_Context.LevelCatalog != null ? m_Context.LevelCatalog.LastLevelIndex : m_LevelIndex;
                    bool goNext = !m_ReturnToMenu && m_LevelIndex < lastLevelIndex;
                    win.Show(
                        m_LastRunEnded.Score,
                        m_LastRunEnded.BestCombo,
                        goNext ? () => Next().Forget() : () => GoToMenu().Forget());
                }

                return;
            }

            if (opened.Panel != EUIPanel.Lose || m_LastRunEnded.IsComplete)
                return;

            LoseCanvasUI lose = opened.Instance.GetComponent<LoseCanvasUI>();
            if (lose != null)
            {
                ISaveService save = m_Context.Services.Get<ISaveService>();
                save.RefreshHearts();
                lose.Show(
                    m_LastRunEnded.Score,
                    m_LastRunEnded.BestCombo,
                    save.Hearts > 0 ? () => Retry().Forget() : null,
                    () => GoToMenu().Forget());
            }
        }

        private void OpenSettings()
        {
            m_Context.Services.Get<IAudioService>().Play(EAudioName.SfxUiClick);
            ShowPausePanel();
        }

        private void ShowPausePanel()
        {
            if (m_SettingsOpen)
                return;

            m_SettingsOpen = true;
            if (m_LevelSystem != null)
                m_LevelSystem.Pause();

            EB.Presentation.Invoke(new OpenUIPanelEvent(EUIPanel.Settings, 1, additive: true));
        }

        private void CloseSettings()
        {
            m_SettingsOpen = false;
            UIPanels.Close(EUIPanel.Settings);
            if (m_LevelSystem != null)
                m_LevelSystem.Resume();
        }

        private void OnApplicationPauseChanged(ApplicationPauseChanged evt)
        {
            if (m_LevelSystem == null || m_HasRunEnded)
                return;

            if (evt.Paused)
            {
                m_LevelSystem.Pause();
                return;
            }

            ShowPausePanel();
        }

        private async UniTaskVoid Next()
        {
            m_Context.GameplayLevelIndex = m_LevelIndex + 1;
            ISceneService scenes = m_Context.Services.Get<ISceneService>();
            await scenes.Reload(ESceneName.Gameplay, m_Context.CancellationToken);
        }

        private async UniTaskVoid Retry()
        {
            ISaveService save = m_Context.Services.Get<ISaveService>();
            save.RefreshHearts();
            if (save.Hearts <= 0)
            {
                GoToMenu().Forget();
                return;
            }

            m_Context.GameplayLevelIndex = m_LevelIndex;
            ISceneService scenes = m_Context.Services.Get<ISceneService>();
            await scenes.Reload(ESceneName.Gameplay, m_Context.CancellationToken);
        }

        private async UniTaskVoid QuitMatchToMenu()
        {
            if (!m_HasRunEnded)
            {
                ISaveService save = m_Context.Services.Get<ISaveService>();
                save.TrySpendHeart();
            }

            await GoToMenu();
        }

        private async UniTask GoToMenu()
        {
            m_Context.GameplayLevelIndex = -1;
            ISceneService scenes = m_Context.Services.Get<ISceneService>();
            await scenes.Load(ESceneName.Menu, m_Context.CancellationToken);
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Shutdown()
        {
            EB.Presentation.Remove<RunEnded>(OnRunEnded);
            EB.Presentation.Remove<UIPanelOpened>(OnPanelOpened);
            EB.Presentation.Remove<ApplicationPauseChanged>(OnApplicationPauseChanged);
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(1));

            if (m_Cts != null)
            {
                m_Cts.Cancel();
                m_Cts.Dispose();
                m_Cts = null;
            }

            m_SettingsOpen = false;

            if (m_PowerSystem != null)
            {
                m_Context.GameLoop.Unregister(m_PowerSystem);
                m_PowerSystem.Shutdown();
                m_PowerSystem = null;
            }

            if (m_SpawnSystem != null)
            {
                m_SpawnSystem.DespawnSeated(m_TargetSystem);
                m_SpawnSystem.DespawnAll();
                m_SpawnSystem = null;
            }

            if (m_CapsuleSystem != null)
            {
                m_Context.GameLoop.Unregister(m_CapsuleSystem);
                m_CapsuleSystem.Shutdown();
                m_CapsuleSystem = null;
            }

            if (m_TargetSystem != null)
            {
                m_TargetSystem.Shutdown();
                m_TargetSystem = null;
            }

            if (m_LevelSystem != null)
            {
                m_Context.GameLoop.Unregister(m_LevelSystem);
                m_LevelSystem.Shutdown();
                m_LevelSystem = null;
            }

            if (m_Feedback != null)
            {
                m_Feedback.Shutdown();
                m_Feedback = null;
            }

            if (m_Assets != null && m_LayoutInstance != null)
            {
                m_Assets.ReleaseInstance(m_LayoutInstance);
                m_LayoutInstance = null;
            }
        }
    }
}
