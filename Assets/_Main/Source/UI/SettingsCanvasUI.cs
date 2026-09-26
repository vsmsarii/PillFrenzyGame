using System;
using PillFrenzy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class SettingsCanvasUI : MonoBehaviour
    {
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_MusicButton;
        [SerializeField] private Button m_SoundButton;
        [SerializeField] private Button m_BackToMenuButton;
        [SerializeField] private TMP_Text m_MusicLabel;
        [SerializeField] private TMP_Text m_SoundLabel;

        private IAudioService m_Audio;
        private Action m_Close;
        private Action m_BackToMenu;

        private void Awake()
        {
            m_CloseButton.onClick.AddListener(OnCloseClicked);
            m_MusicButton.onClick.AddListener(OnMusicClicked);
            m_SoundButton.onClick.AddListener(OnSoundClicked);
            m_BackToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        public void Bind(IAudioService audio, Action close, Action backToMenu = null)
        {
            m_Audio = audio;
            m_Close = close;
            m_BackToMenu = backToMenu;
            m_BackToMenuButton.gameObject.SetActive(backToMenu != null);
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            m_MusicLabel.text = m_Audio.MusicMuted ? "MUSIC OFF" : "MUSIC ON";
            m_SoundLabel.text = m_Audio.SoundMuted ? "SOUND OFF" : "SOUND ON";
        }

        private void OnMusicClicked()
        {
            m_Audio.SetMusicMuted(!m_Audio.MusicMuted);
            m_Audio.Play(EAudioName.SfxUiClick);
            RefreshLabels();
        }

        private void OnSoundClicked()
        {
            m_Audio.SetSoundMuted(!m_Audio.SoundMuted);
            m_Audio.Play(EAudioName.SfxUiClick);
            RefreshLabels();
        }

        private void OnCloseClicked()
        {
            m_Audio.Play(EAudioName.SfxUiClick);
            m_Close.Invoke();
        }

        private void OnBackToMenuClicked()
        {
            m_Audio.Play(EAudioName.SfxUiClick);
            m_BackToMenu?.Invoke();
        }
    }
}
