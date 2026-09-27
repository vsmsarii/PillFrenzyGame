using System.Threading;
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
        private ISaveService m_Save;
        private GameplaySession m_Session;
        private GameplayPauseMenu m_PauseMenu;
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
            if (m_Context != null)
                BootAsync().Forget();
        }

        private async UniTaskVoid BootAsync()
        {
            await AppInstaller.LoadGlobalsAsync(m_Context);

            m_Save = m_Context.Services.Get<ISaveService>();
            RefillHeartsIfEmpty();
            m_PauseMenu = new GameplayPauseMenu(m_Context.Services.Get<IAudioService>(), () => RestartAsync(m_Level).Forget());

            await StartLevelAsync(m_Level);
        }

        private async UniTask StartLevelAsync(int levelNumber)
        {
            m_Level = Mathf.Clamp(levelNumber, 1, m_Context.LevelCatalog.LevelCount);

            m_Session?.Shutdown();
            GameplaySession session = new GameplaySession(m_Context);
            m_Session = session;
            m_PauseMenu.Track(session);

            await UIPanels.ShowLoading(session.Token);
            if (!await session.LoadAsync(m_Level - 1))
                return;

            if (!await GameplayScreens.OpenHudAsync(session, m_Level, m_Save, m_PauseMenu.OpenFromButton))
                return;

            UIPanels.HideLoading();
            RunSessionAsync(session, m_Level).Forget();
        }

        private async UniTaskVoid RunSessionAsync(GameplaySession session, int levelNumber)
        {
            if (!await session.StartAsync())
                return;

            (bool canceled, RunEnded result) = await session.WaitForRunEndAsync();
            if (canceled)
                return;

            RefillHeartsIfEmpty();
            int nextLevel = Mathf.Min(levelNumber + 1, m_Context.LevelCatalog.LevelCount);
            await GameplayScreens.ShowResultAsync(
                result,
                () => ContinueAfterWin(levelNumber, nextLevel, session.Token).Forget(),
                () => RestartAsync(levelNumber).Forget(),
                () => RestartAsync(levelNumber).Forget(),
                session.Token);
        }

        private async UniTaskVoid ContinueAfterWin(int completedLevelNumber, int nextLevelNumber, CancellationToken token)
        {
            IAdService ads = m_Context.Services.Get<IAdService>();
            int completedLevelIndex = completedLevelNumber - 1;
            if (ads.IsDueAfterLevel(completedLevelIndex))
                await ads.ShowAsync(completedLevelIndex, token);

            RestartAsync(nextLevelNumber).Forget();
        }

        private async UniTaskVoid RestartAsync(int levelNumber)
        {
            if (m_Restarting)
                return;

            m_Restarting = true;
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(0));
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(1));
            await StartLevelAsync(levelNumber);
            m_Restarting = false;
        }

        private void RefillHeartsIfEmpty()
        {
            if (m_Save.Hearts <= 0)
                m_Save.GrantHearts(Mathf.Max(1, m_Save.MaxHearts));
        }

        private void OnDestroy()
        {
            m_PauseMenu?.Dispose();
            m_Session?.Shutdown();
            m_Context?.Dispose();
        }
    }
}
