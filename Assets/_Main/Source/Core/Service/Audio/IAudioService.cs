using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Core
{
    public interface IAudioService : IService
    {
        bool MusicMuted { get; }
        bool SoundMuted { get; }

        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        void Play(EAudioName name);
        void PlayMusic(EAudioName name);
        void StopMusic();
        void SetMusicMuted(bool muted);
        void SetSoundMuted(bool muted);
    }
}
