using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class GameplaySession
    {
        private const int ExtraCapsuleWarmup = 2;

        private readonly GameContext m_Context;
        private readonly IAssetProvider m_Assets;
        private readonly CancellationTokenSource m_Cts = new();
        private readonly CancellationToken m_Token;
        private readonly UniTaskCompletionSource<RunEnded> m_RunEnd = new();
        private readonly PreRunSequence m_PreRun = new();
        private readonly List<string> m_LoadedAssetKeys = new();

        private GameObject m_LayoutInstance;
        private TargetSystem m_Targets;
        private CapsuleSystem m_Capsules;
        private SpawnSystem m_Spawn;
        private SpawnPacingSystem m_Pacing;
        private RunScoreSystem m_Score;
        private GameplayFeedback m_Feedback;
        private VfxSystem m_Vfx;
        private LevelCameraFramer m_CameraFramer;
        private TutorialRunSystem m_TutorialRun;
        private bool m_ShutDown;

        public LevelSystem Level { get; private set; }
        public SpecialPowerSystem Powers { get; private set; }
        public LevelDefinitionSO Definition { get; private set; }
        public bool IsLoaded { get; private set; }
        public bool HasRunEnded => Level != null && Level.HasEnded;
        public bool IsRunActive => Level != null && Level.Phase == ELevelPhase.Playing;
        public CancellationToken Token => m_Token;

        public GameplaySession(GameContext context)
        {
            m_Context = context;
            m_Assets = context.Services.Get<IAssetProvider>();
            m_Token = m_Cts.Token;
        }

        public async UniTask<bool> LoadAsync(int levelIndex)
        {
            UIPanels.SetLoadingProgress(0.8f);
            TargetCatalogSO targetCatalog = await LoadAsset<TargetCatalogSO>(AddressableKeys.TargetCatalog);
            if (targetCatalog == null)
                return AbortLoad("Target catalog missing.");

            UIPanels.SetLoadingProgress(0.85f);
            if (!m_Context.LevelCatalog.TryGetDefinitionKey(levelIndex, out string definitionKey))
                return AbortLoad("Level definition key missing for index " + levelIndex + ".");

            Definition = await LoadAsset<LevelDefinitionSO>(definitionKey);
            if (Definition == null)
                return AbortLoad("Level definition missing for index " + levelIndex + ".");

            UIPanels.SetLoadingProgress(0.9f);
            if (!Definition.TryGetLayoutKey(out string layoutKey) && !m_Context.LevelCatalog.TryGetDefaultLayoutKey(out layoutKey))
                return AbortLoad("Level layout missing for index " + levelIndex + ".");

            m_LayoutInstance = await m_Assets.Instantiate(layoutKey, null, m_Token);
            if (m_LayoutInstance == null)
                return AbortLoad("Level layout failed to instantiate for index " + levelIndex + ".");

            LevelLayout layout = m_LayoutInstance.GetComponent<LevelLayout>();
            if (layout.Paths.Count == 0)
                return AbortLoad("Level layout has no paths.");

            FeedbackSettingsSO feedbackSettings = await LoadAsset<FeedbackSettingsSO>(AddressableKeys.FeedbackSettings);
            SpecialPowerCatalogSO powerCatalog = await LoadAsset<SpecialPowerCatalogSO>(AddressableKeys.SpecialPowerCatalog);
            VfxCatalogSO vfxCatalog = await LoadAsset<VfxCatalogSO>(AddressableKeys.VfxCatalog);
            TutorialCatalogSO tutorialCatalog = await LoadAsset<TutorialCatalogSO>(AddressableKeys.TutorialCatalog);
            if (feedbackSettings == null || powerCatalog == null)
                return AbortLoad("Feedback settings or special power catalog missing.");

            if (m_Token.IsCancellationRequested)
                return false;

            ISaveService save = m_Context.Services.Get<ISaveService>();
            IAudioService audio = m_Context.Services.Get<IAudioService>();
            IGameObjectPool pool = m_Context.Services.Get<IGameObjectPool>();
            Camera camera = Camera.main;
            audio.PlayMusic(EAudioName.MusicGameplay);

            if (vfxCatalog == null)
                Logger.Warning("VFX catalog missing. Gameplay runs without VFX.");

            m_Vfx = new VfxSystem(pool, vfxCatalog != null ? vfxCatalog.Entries : Array.Empty<VfxEntry>(), m_Token);
            Transform shakeTarget = layout.ShakeHolder != null ? layout.ShakeHolder : camera.transform;
            m_Feedback = new GameplayFeedback(m_Vfx, audio, shakeTarget, feedbackSettings);
            m_Targets = new TargetSystem(new TargetFactory(pool, targetCatalog));
            m_CameraFramer = new LevelCameraFramer(camera, layout, Definition, m_Context.GlobalSettings, targetCatalog);

            UIPanels.SetLoadingProgress(0.95f);
            await m_Targets.Bind(Definition, layout, m_Token);
            await pool.Warmup(AddressableKeys.CapsulePrefab, CapsuleWarmupCount(targetCatalog), m_Token);
            await m_Vfx.Warmup();
            if (m_Token.IsCancellationRequested)
                return false;

            m_Capsules = new CapsuleSystem(m_Context.Services.Get<IInputService>(), m_Targets, camera, m_Token, m_Feedback, layout.CapsuleMask);
            m_Spawn = new SpawnSystem(new CapsuleFactory(pool, layout.CapsuleRoot), m_Capsules);
            m_Score = new RunScoreSystem(save);
            Level = new LevelSystem(m_Score, save, levelIndex, m_Feedback);
            m_Pacing = new SpawnPacingSystem(m_Spawn, m_Capsules, layout.Paths, Level, m_Token);
            m_Capsules.Bind(m_Spawn, Level);

            Powers = new SpecialPowerSystem(powerCatalog, save, Level, m_Pacing);
            Powers.SyncUnlockGrants();

            if (tutorialCatalog != null)
                SetupTutorials(tutorialCatalog, levelIndex, save, camera);
            else
                Logger.Warning("Tutorial catalog missing. Gameplay runs without tutorials.");

            GameLoop loop = m_Context.GameLoop;
            loop.Register(m_CameraFramer);
            loop.Register(m_Capsules);
            loop.Register(Level);
            loop.Register(m_Pacing);
            loop.Register(Powers);
            loop.Register(m_Vfx);
            if (m_TutorialRun != null)
                loop.Register(m_TutorialRun);

            UIPanels.SetLoadingProgress(1f);
            IsLoaded = true;
            return true;
        }

        public async UniTask<bool> StartAsync()
        {
            m_Targets.PublishFill();
            Level.EnterIntro();

            PreRunContext context = new PreRunContext(Level.LevelIndex, Definition);
            bool canceled = await m_PreRun.RunAsync(context, m_Token).SuppressCancellationThrow();
            if (canceled)
                return false;

            EB.Presentation.Add<RunEnded>(OnRunEnded);
            return Level.StartRun(Definition);
        }

        public async UniTask<(bool Canceled, RunEnded Result)> WaitForRunEndAsync()
        {
            return await m_RunEnd.Task.AttachExternalCancellation(m_Token).SuppressCancellationThrow();
        }

        public void Pause(EPauseReason reason)
        {
            Level.Pause(reason);
        }

        public void Resume(EPauseReason reason)
        {
            Level.Resume(reason);
        }

        public void Shutdown()
        {
            if (m_ShutDown)
                return;

            m_ShutDown = true;
            IsLoaded = false;
            EB.Presentation.Remove<RunEnded>(OnRunEnded);
            m_Cts.Cancel();
            m_Cts.Dispose();
            m_RunEnd.TrySetCanceled();
            m_PreRun.Clear();

            GameLoop loop = m_Context.GameLoop;
            loop.Unregister(m_CameraFramer);
            loop.Unregister(m_Capsules);
            loop.Unregister(Level);
            loop.Unregister(m_Pacing);
            loop.Unregister(Powers);
            loop.Unregister(m_Vfx);
            loop.Unregister(m_TutorialRun);

            Powers?.Shutdown();
            m_TutorialRun?.Shutdown();
            m_Pacing?.Shutdown();
            m_Spawn?.DespawnSeated(m_Targets);
            m_Spawn?.DespawnAll();
            m_Capsules?.Shutdown();
            m_Targets?.Shutdown();
            Level?.Shutdown();
            m_Score?.Shutdown();
            m_Feedback?.Shutdown();
            m_Vfx?.Shutdown();

            if (m_LayoutInstance != null)
                m_Assets.ReleaseInstance(m_LayoutInstance);

            foreach (string key in m_LoadedAssetKeys)
                m_Assets.ReleaseAsset(key);

            m_LoadedAssetKeys.Clear();
        }

        private void SetupTutorials(TutorialCatalogSO catalog, int levelIndex, ISaveService save, Camera camera)
        {
            TutorialSelector selector = new TutorialSelector(catalog, save, Powers);
            TutorialPresenter modalPresenter = new TutorialPresenter();
            PrepareForPendingTutorials(selector, levelIndex);

            m_PreRun.Add(new TutorialPreRunStep(selector, modalPresenter, save));
            m_TutorialRun = new TutorialRunSystem(
                selector,
                new TutorialMomentPresenter(m_Token),
                modalPresenter,
                save,
                Level,
                m_Capsules,
                camera,
                m_Token);
            m_Capsules.SetTapInterceptor(m_TutorialRun);
        }

        private void PrepareForPendingTutorials(TutorialSelector selector, int levelIndex)
        {
            List<TutorialDefinitionSO> pending = new List<TutorialDefinitionSO>();
            selector.CollectPending(levelIndex, Definition, pending);
            foreach (TutorialDefinitionSO tutorial in pending)
            {
                if(tutorial == null) 
                    continue;

                if (tutorial.RevealsSpecialPower)
                    Powers.Conceal(tutorial.SpecialPower);

                if(tutorial.ForcedSpawns == null)
                    continue;
                foreach (ForcedCapsuleSpawn forcedSpawn in tutorial.ForcedSpawns)
                    m_Pacing.ForceKind(forcedSpawn);
            }
        }

        private void OnRunEnded(RunEnded evt)
        {
            EB.Presentation.Remove<RunEnded>(OnRunEnded);
            m_RunEnd.TrySetResult(evt);
        }

        private int CapsuleWarmupCount(TargetCatalogSO targetCatalog)
        {
            int largestBox = 0;
            foreach (TargetQuota quota in Definition.TargetQueue)
                largestBox = Mathf.Max(largestBox, (int)quota.Capacity);

            return Definition.MaxActive + targetCatalog.VisibleCount * largestBox + ExtraCapsuleWarmup;
        }

        private async UniTask<T> LoadAsset<T>(string key) where T : UnityEngine.Object
        {
            T asset = await m_Assets.LoadAsset<T>(key, m_Token);
            if (asset != null)
                m_LoadedAssetKeys.Add(key);

            return asset;
        }

        private bool AbortLoad(string reason)
        {
            if (m_Token.IsCancellationRequested)
                return false;

            Logger.Error(reason);
            UIPanels.HideLoading();
            return false;
        }
    }
}
