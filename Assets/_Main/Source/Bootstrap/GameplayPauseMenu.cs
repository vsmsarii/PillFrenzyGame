using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class GameplayPauseMenu : IDisposable
    {
        private const int PanelLayer = 1;

        private readonly IAudioService m_Audio;
        private readonly Action m_LeaveMatch;
        private GameplaySession m_Session;
        private bool m_IsOpen;

        public GameplayPauseMenu(IAudioService audio, Action leaveMatch)
        {
            m_Audio = audio;
            m_LeaveMatch = leaveMatch;
            EB.Presentation.Add<ApplicationPauseChanged>(OnApplicationPauseChanged);
        }

        public void Track(GameplaySession session)
        {
            m_Session = session;
            m_IsOpen = false;
        }

        public void OpenFromButton()
        {
            m_Audio.Play(EAudioName.SfxUiClick);
            Open();
        }

        public void Dispose()
        {
            EB.Presentation.Remove<ApplicationPauseChanged>(OnApplicationPauseChanged);
        }

        private void Open()
        {
            if (m_IsOpen)
                return;

            m_IsOpen = true;
            m_Session.Pause(EPauseReason.Menu);
            ShowPanelAsync(m_Session.Token).Forget();
        }

        private async UniTaskVoid ShowPanelAsync(CancellationToken token)
        {
            GameObject panel = await UIPanels.OpenAsync(EUIPanel.Settings, PanelLayer, additive: true, cancellationToken: token);
            if (panel != null)
                panel.GetComponent<SettingsCanvasUI>().Bind(m_Audio, Close, m_LeaveMatch);
        }

        private void Close()
        {
            m_IsOpen = false;
            UIPanels.Close(EUIPanel.Settings);
            m_Session.Resume(EPauseReason.Menu);
        }

        private void OnApplicationPauseChanged(ApplicationPauseChanged evt)
        {
            if (m_Session == null || !m_Session.IsLoaded || m_Session.HasRunEnded)
                return;

            if (evt.Paused)
            {
                m_Session.Pause(EPauseReason.Application);
                return;
            }

            Open();
            m_Session.Resume(EPauseReason.Application);
        }
    }
}
