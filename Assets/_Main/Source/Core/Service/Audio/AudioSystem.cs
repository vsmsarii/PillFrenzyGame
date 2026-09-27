using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PillFrenzy.Core
{
    public sealed class AudioSystem : Service, IAudioService
    {
        private const string MusicMuteKey = "pillfrenzy.audio.music.mute";
        private const string SoundMuteKey = "pillfrenzy.audio.sound.mute";

        private readonly IAssetProvider m_Assets;
        private readonly Dictionary<EAudioName, AudioClip> m_Clips = new();
        private GameObject m_Root;
        private AudioSource m_SfxSource;
        private AudioSource m_MusicSource;
        private EAudioName m_CurrentMusic;
        private bool m_MusicMuted;
        private bool m_SoundMuted;

        public bool MusicMuted => m_MusicMuted;
        public bool SoundMuted => m_SoundMuted;

        public AudioSystem(IAssetProvider assets)
        {
            m_Assets = assets;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            AudioCatalogSO catalog = await m_Assets.LoadAsset<AudioCatalogSO>(AddressableKeys.AudioCatalog, cancellationToken);
            m_Clips.Clear();
            foreach (AudioCatalogEntry entry in catalog.Entries)
            {
                if (entry.Clip != null)
                    m_Clips[entry.Name] = entry.Clip;
            }
        }

        public void Play(EAudioName name)
        {
            if (!m_SoundMuted && m_Clips.TryGetValue(name, out AudioClip clip))
                m_SfxSource.PlayOneShot(clip);
        }

        public void PlayMusic(EAudioName name)
        {
            if (m_CurrentMusic == name && m_MusicSource.isPlaying)
                return;

            if (!m_Clips.TryGetValue(name, out AudioClip clip))
            {
                StopMusic();
                return;
            }

            m_CurrentMusic = name;
            m_MusicSource.clip = clip;
            m_MusicSource.Play();
        }

        public void StopMusic()
        {
            m_CurrentMusic = EAudioName.None;
            m_MusicSource.Stop();
            m_MusicSource.clip = null;
        }

        public void SetMusicMuted(bool muted)
        {
            m_MusicMuted = muted;
            PlayerPrefs.SetInt(MusicMuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            m_MusicSource.mute = muted;
        }

        public void SetSoundMuted(bool muted)
        {
            m_SoundMuted = muted;
            PlayerPrefs.SetInt(SoundMuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            m_SfxSource.mute = muted;
        }

        protected override void OnInitialize()
        {
            m_MusicMuted = PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;
            m_SoundMuted = PlayerPrefs.GetInt(SoundMuteKey, 0) == 1;

            m_Root = new GameObject("[Audio]");
            Object.DontDestroyOnLoad(m_Root);
            m_SfxSource = m_Root.AddComponent<AudioSource>();
            m_SfxSource.playOnAwake = false;
            m_SfxSource.spatialBlend = 0f;
            m_SfxSource.mute = m_SoundMuted;

            m_MusicSource = m_Root.AddComponent<AudioSource>();
            m_MusicSource.playOnAwake = false;
            m_MusicSource.spatialBlend = 0f;
            m_MusicSource.loop = true;
            m_MusicSource.mute = m_MusicMuted;

            EB.Presentation.Add<AdStarted>(OnAdStarted);
            EB.Presentation.Add<AdFinished>(OnAdFinished);
        }

        protected override void OnDispose()
        {
            EB.Presentation.Remove<AdStarted>(OnAdStarted);
            EB.Presentation.Remove<AdFinished>(OnAdFinished);
            AudioListener.pause = false;
            m_Clips.Clear();
            Object.Destroy(m_Root);
        }

        private void OnAdStarted(AdStarted evt)
        {
            AudioListener.pause = true;
        }

        private void OnAdFinished(AdFinished evt)
        {
            AudioListener.pause = false;
        }
    }
}
