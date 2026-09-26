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
        private GameplaySession m_Session;
        private int m_LevelIndex;
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
        }

        private void Start()
        {
            if (m_Context == null)
                return;

            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            ISaveService save = m_Context.Services.Get<ISaveService>();

            m_LevelIndex = m_Context.GameplayLevelIndex >= 0 ? m_Context.GameplayLevelIndex : save.CurrentLevelIndex;
            if (m_Context.LevelCatalog != null && m_LevelIndex > m_Context.LevelCatalog.LastLevelIndex)
                m_LevelIndex = m_Context.LevelCatalog.LastLevelIndex;
            if (m_LevelIndex < 0)
                m_LevelIndex = 0;

            m_Session = new GameplaySession(m_Context);
            if (!await m_Session.LoadAsync(m_LevelIndex))
                return;

            m_ReturnToMenu = m_Session.Definition.ReturnToMenu;

            EB.Presentation.Add<RunEnded>(OnRunEnded);
            EB.Presentation.Add<UIPanelOpened>(OnPanelOpened);
            EB.Presentation.Add<ApplicationPauseChanged>(OnApplicationPauseChanged);
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
                m_Session.BeginRun();

                GameplayCanvasUI gameplayUi = opened.Instance.GetComponent<GameplayCanvasUI>();
                if (gameplayUi != null)
                {
                    gameplayUi.BindLevel(m_LevelIndex + 1);
                    if (m_Session.Powers != null)
                        gameplayUi.BindPowers(m_Session.Powers, m_Context.Services.Get<ISaveService>());
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
            m_Session?.Pause();
            EB.Presentation.Invoke(new OpenUIPanelEvent(EUIPanel.Settings, 1, additive: true));
        }

        private void CloseSettings()
        {
            m_SettingsOpen = false;
            UIPanels.Close(EUIPanel.Settings);
            m_Session?.Resume();
        }

        private void OnApplicationPauseChanged(ApplicationPauseChanged evt)
        {
            if (m_Session == null || !m_Session.IsLoaded || m_HasRunEnded)
                return;

            if (evt.Paused)
            {
                m_Session.Pause();
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

        private void OnDestroy()
        {
            EB.Presentation.Remove<RunEnded>(OnRunEnded);
            EB.Presentation.Remove<UIPanelOpened>(OnPanelOpened);
            EB.Presentation.Remove<ApplicationPauseChanged>(OnApplicationPauseChanged);
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(1));

            m_SettingsOpen = false;
            m_Session?.Shutdown();
            m_Session = null;
        }
    }
}
