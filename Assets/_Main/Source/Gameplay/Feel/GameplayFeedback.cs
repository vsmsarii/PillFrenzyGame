using DG.Tweening;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class GameplayFeedback
    {
        private readonly VfxSystem m_Vfx;
        private readonly IAudioService m_Audio;
        private readonly Transform m_ShakeTarget;
        private readonly Vector3 m_ShakeOrigin;
        private readonly FeedbackSettingsSO m_Settings;

        private Tween m_ShakeTween;

        public GameplayFeedback(
            VfxSystem vfx,
            IAudioService audio,
            Transform shakeTarget,
            FeedbackSettingsSO settings)
        {
            m_Vfx = vfx;
            m_Audio = audio;
            m_ShakeTarget = shakeTarget;
            m_ShakeOrigin = shakeTarget.localPosition;
            m_Settings = settings;
        }

        public void PlayCorrect(Vector3 position, Color color)
        {
            Play(EAudioName.SfxCorrect, m_Settings.Correct);
            m_Vfx.Play(EVfxId.CapsuleCorrect, position, color);
        }

        public void PlayGold(Vector3 position, Color color)
        {
            Play(EAudioName.SfxGold, m_Settings.Gold);
            m_Vfx.Play(EVfxId.CapsuleGold, position, color);
        }

        public void PlayPoison(Vector3 position, Color color)
        {
            Play(EAudioName.SfxPoison, m_Settings.Poison);
            m_Vfx.Play(EVfxId.CapsulePoison, position, color);
        }

        public void PlayComplete()
        {
            Play(EAudioName.SfxComplete, m_Settings.Complete);
        }

        public void PlayFail()
        {
            Play(EAudioName.SfxFail, m_Settings.Fail);
        }

        public void Shutdown()
        {
            KillShake();
        }

        private void Play(EAudioName sound, FeedbackProfile profile)
        {
            m_Audio.Play(sound);
            Shake(profile);
        }

        private void Shake(FeedbackProfile profile)
        {
            if (profile.ShakeDuration <= 0f)
                return;

            KillShake();
            m_ShakeTween = m_ShakeTarget
                .DOShakePosition(profile.ShakeDuration, profile.ShakeStrength, m_Settings.ShakeVibrato, m_Settings.ShakeRandomness, false, true)
                .SetLink(m_ShakeTarget.gameObject);
        }

        private void KillShake()
        {
            if (m_ShakeTween != null && m_ShakeTween.IsActive())
                m_ShakeTween.Kill();

            m_ShakeTween = null;

            if (m_ShakeTarget != null)
                m_ShakeTarget.localPosition = m_ShakeOrigin;
        }
    }
}
