using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class BootLevelTest : MonoBehaviour
    {
        [SerializeField] private int m_Level = 1;

        private GameContext m_Context;
        private GameplaySession m_Session;
        private RunEnded m_LastRunEnded;
        private bool m_HasRunEnded;
        private bool m_SettingsOpen;
        private bool m_Booted;
        private bool m_Restarting;

        private void Awake()
        {
            if (GameRunner.Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            m_Context = AppInstaller.CreateContext(gameObject);
        }

        private void Start()
        {
            if (m_Context == null)
                return;

            BootAsync().Forget();
        }

        private async UniTaskVoid BootAsync()
        {
            await AppInstaller.LoadGlobalsAsync(m_Context);

            ISaveService save = m_Context.Services.Get<ISaveService>();
            if (save.Hearts <= 0)
                save.GrantHearts(Mathf.Max(1, save.MaxHearts));

            EB.Presentation.Add<RunEnded>(OnRunEnded);
            EB.Presentation.Add<UIPanelOpened>(OnPanelOpened);
            EB.Presentation.Add<ApplicationPauseChanged>(OnApplicationPauseChanged);

            m_Booted = true;
            await StartLevelAsync(ResolveLevelNumber(m_Level));
        }

        private int ResolveLevelNumber(int levelNumber)
        {
            int level = levelNumber < 1 ? 1 : levelNumber;
            if (m_Context.LevelCatalog != null)
            {
                int maxLevel = m_Context.LevelCatalog.LevelCount;
                if (maxLevel > 0 && level > maxLevel)
                    level = maxLevel;
            }

            return level;
        }

        private async UniTask StartLevelAsync(int levelNumber)
        {
            m_Level = levelNumber;
            m_HasRunEnded = false;
            m_SettingsOpen = false;
            m_LastRunEnded = default;

            TeardownSession();
            GameplaySession session = new GameplaySession(m_Context);
            m_Session = session;

            await UIPanels.ShowLoading(m_Context.CancellationToken);
            if (m_Session != session)
                return;

            if (!await session.LoadAsync(levelNumber - 1) || m_Session != session)
                return;

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
            if (opened.Instance == null || m_Session == null || !m_Session.IsLoaded)
                return;

            if (opened.Panel == EUIPanel.Gameplay)
            {
                m_Session.BeginRun();

                GameplayCanvasUI gameplayUi = opened.Instance.GetComponent<GameplayCanvasUI>();
                if (gameplayUi != null)
                {
                    gameplayUi.BindLevel(m_Level);
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
                        () => RestartAsync(m_Level).Forget());
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
                    int lastLevel = m_Context.LevelCatalog != null ? m_Context.LevelCatalog.LevelCount : m_Level;
                    bool goNext = m_Level < lastLevel;
                    win.Show(
                        m_LastRunEnded.Score,
                        m_LastRunEnded.BestCombo,
                        goNext ? () => RestartAsync(m_Level + 1).Forget() : () => RestartAsync(m_Level).Forget());
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
                if (save.Hearts <= 0)
                    save.GrantHearts(Mathf.Max(1, save.MaxHearts));

                lose.Show(
                    m_LastRunEnded.Score,
                    m_LastRunEnded.BestCombo,
                    () => RestartAsync(m_Level).Forget(),
                    () => RestartAsync(m_Level).Forget());
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

        private async UniTaskVoid RestartAsync(int levelNumber)
        {
            if (m_Restarting || !m_Booted)
                return;

            m_Restarting = true;
            TeardownSession();
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(0));
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(1));
            await StartLevelAsync(ResolveLevelNumber(levelNumber));
            m_Restarting = false;
        }

        private void TeardownSession()
        {
            m_SettingsOpen = false;
            m_Session?.Shutdown();
            m_Session = null;
        }

        private void OnDestroy()
        {
            EB.Presentation.Remove<RunEnded>(OnRunEnded);
            EB.Presentation.Remove<UIPanelOpened>(OnPanelOpened);
            EB.Presentation.Remove<ApplicationPauseChanged>(OnApplicationPauseChanged);
            TeardownSession();

            if (m_Context != null)
            {
                m_Context.Dispose();
                m_Context = null;
            }
        }
    }
}
