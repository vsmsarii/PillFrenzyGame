using System;
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
        private ISaveService m_Save;
        private GameplaySession m_Session;
        private GameplayPauseMenu m_PauseMenu;
        private int m_LevelIndex;

        private void Start()
        {
            if (GameRunner.Instance == null)
            {
                Logger.Error("GameRunner missing. Play from Init.");
                return;
            }

            m_Context = GameRunner.Instance.Context;
            m_Save = m_Context.Services.Get<ISaveService>();
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            int requestedIndex = m_Context.GameplayLevelIndex >= 0 ? m_Context.GameplayLevelIndex : m_Save.CurrentLevelIndex;
            m_LevelIndex = Mathf.Min(requestedIndex, m_Context.LevelCatalog.LastLevelIndex);

            m_Session = new GameplaySession(m_Context);
            m_PauseMenu = new GameplayPauseMenu(m_Context.Services.Get<IAudioService>(), () => QuitMatchToMenu().Forget());
            m_PauseMenu.Track(m_Session);

            if (!await m_Session.LoadAsync(m_LevelIndex))
                return;

            if (!await GameplayScreens.OpenHudAsync(m_Session, m_LevelIndex + 1, m_Save, m_PauseMenu.OpenFromButton))
                return;

            UIPanels.HideLoading();

            if (!await m_Session.StartAsync())
                return;

            (bool canceled, RunEnded result) = await m_Session.WaitForRunEndAsync();
            if (canceled)
                return;

            m_Save.RefreshHearts();
            bool hasNextLevel = !m_Session.Definition.ReturnToMenu && m_LevelIndex < m_Context.LevelCatalog.LastLevelIndex;
            Action onContinue = () => ContinueAfterWin(hasNextLevel).Forget();
            Action onRetry = m_Save.Hearts > 0 ? () => LoadLevel(m_LevelIndex).Forget() : null;

            await GameplayScreens.ShowResultAsync(result, onContinue, onRetry, () => GoToMenu().Forget(), m_Session.Token);
        }

        private async UniTaskVoid ContinueAfterWin(bool hasNextLevel)
        {
            IAdService ads = m_Context.Services.Get<IAdService>();
            if (ads.IsDueAfterLevel(m_LevelIndex))
                await ads.ShowAsync(m_LevelIndex, m_Session.Token);

            if (hasNextLevel)
                await LoadLevel(m_LevelIndex + 1);
            else
                await GoToMenu();
        }

        private async UniTaskVoid QuitMatchToMenu()
        {
            if (m_Session.IsRunActive)
                m_Save.TrySpendHeart();

            await GoToMenu();
        }

        private UniTask LoadLevel(int levelIndex)
        {
            m_Context.GameplayLevelIndex = levelIndex;
            return LeaveTo(ESceneName.Gameplay);
        }

        private UniTask GoToMenu()
        {
            m_Context.GameplayLevelIndex = -1;
            return LeaveTo(ESceneName.Menu);
        }

        private async UniTask LeaveTo(ESceneName scene)
        {
            await UIPanels.ShowLoading(m_Context.CancellationToken);
            m_Session.Shutdown();
            await m_Context.Services.Get<ISceneService>().Load(scene, m_Context.CancellationToken);
        }

        private void OnDestroy()
        {
            EB.Presentation.Invoke(new CloseAllUIPanelsEvent(1));
            m_PauseMenu?.Dispose();
            m_Session?.Shutdown();
        }
    }
}
