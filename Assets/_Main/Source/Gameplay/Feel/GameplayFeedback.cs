using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class GameplayFeedback
    {
        private readonly IGameObjectPool m_Pool;
        private readonly IAudioService m_Audio;
        private readonly Transform m_ShakeTarget;
        private readonly Vector3 m_ShakeOrigin;
        private readonly FeedbackSettingsSO m_Settings;
        private readonly CancellationToken m_Token;

        private Tween m_ShakeTween;

        public GameplayFeedback(
            IGameObjectPool pool,
            IAudioService audio,
            Transform shakeTarget,
            FeedbackSettingsSO settings,
            CancellationToken token)
        {
            m_Pool = pool;
            m_Audio = audio;
            m_ShakeTarget = shakeTarget;
            m_ShakeOrigin = shakeTarget != null ? shakeTarget.localPosition : Vector3.zero;
            m_Settings = settings;
            m_Token = token;
        }

        public void PlayCorrect(Vector3 position)
        {
            Play(EAudioName.SfxCorrect, Profile(EFeedbackKind.Correct), AddressableKeys.VfxPop, position);
        }

        public void PlayGold(Vector3 position)
        {
            Play(EAudioName.SfxGold, Profile(EFeedbackKind.Gold), AddressableKeys.VfxGold, position);
        }

        public void PlayPoison(Vector3 position)
        {
            Play(EAudioName.SfxPoison, Profile(EFeedbackKind.Poison), AddressableKeys.VfxPoison, position);
        }

        public void PlayComplete()
        {
            Play(EAudioName.SfxComplete, Profile(EFeedbackKind.Complete), null, Vector3.zero);
        }

        public void PlayFail()
        {
            Play(EAudioName.SfxFail, Profile(EFeedbackKind.Fail), null, Vector3.zero);
        }

        public void Shutdown()
        {
            KillShake();
        }

        private void Play(EAudioName sound, FeedbackProfile profile, string vfxKey, Vector3 position)
        {
            if (m_Audio != null)
                m_Audio.Play(sound);

            Shake(profile);

            if (profile.HasBurst && !string.IsNullOrEmpty(vfxKey))
                Burst(vfxKey, position, profile).Forget();
        }

        private FeedbackProfile Profile(EFeedbackKind kind)
        {
            if (m_Settings == null)
                return DefaultProfile(kind);

            switch (kind)
            {
                case EFeedbackKind.Correct: return m_Settings.Correct;
                case EFeedbackKind.Gold: return m_Settings.Gold;
                case EFeedbackKind.Poison: return m_Settings.Poison;
                case EFeedbackKind.Complete: return m_Settings.Complete;
                default: return m_Settings.Fail;
            }
        }

        private static FeedbackProfile DefaultProfile(EFeedbackKind kind)
        {
            switch (kind)
            {
                case EFeedbackKind.Correct: return FeedbackProfile.Create(0.16f, 0.08f, true);
                case EFeedbackKind.Gold: return FeedbackProfile.Create(0.18f, 0.1f, true);
                case EFeedbackKind.Poison: return FeedbackProfile.Create(0.28f, 0.22f, true);
                case EFeedbackKind.Complete: return FeedbackProfile.Create(0.32f, 0.14f, false);
                default: return FeedbackProfile.Create(0.36f, 0.18f, false);
            }
        }

        private void Shake(FeedbackProfile profile)
        {
            if (m_ShakeTarget == null || profile.ShakeDuration <= 0f)
                return;

            KillShake();

            int vibrato = m_Settings != null ? m_Settings.ShakeVibrato : 14;
            float randomness = m_Settings != null ? m_Settings.ShakeRandomness : 90f;

            m_ShakeTween = m_ShakeTarget
                .DOShakePosition(profile.ShakeDuration, profile.ShakeStrength, vibrato, randomness, false, true)
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

        private async UniTaskVoid Burst(string key, Vector3 position, FeedbackProfile profile)
        {
            if (m_Pool == null)
                return;

            (bool getCanceled, GameObject instance) = await m_Pool.Get(key, null, m_Token).SuppressCancellationThrow();
            if (getCanceled || instance == null)
            {
                if (instance != null)
                    m_Pool.Release(instance);
                return;
            }

            Transform burst = instance.transform;
            burst.SetPositionAndRotation(position, Quaternion.identity);
            burst.localScale = Vector3.one * profile.BurstStartScale;
            burst.DOScale(profile.BurstEndScale, profile.BurstGrowDuration).SetEase(Ease.OutBack).SetLink(instance);

            int lifetimeMs = Mathf.RoundToInt(profile.BurstLifetimeSeconds * 1000f);
            await UniTask.Delay(lifetimeMs, cancellationToken: m_Token).SuppressCancellationThrow();
            m_Pool.Release(instance);
        }

        private enum EFeedbackKind
        {
            Correct = 0,
            Gold,
            Poison,
            Complete,
            Fail
        }
    }
}
