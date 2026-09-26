using System;
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
        private readonly GameContext m_Context;
        private readonly IAssetProvider m_Assets;
        private readonly CancellationTokenSource m_Cts = new();

        private GameObject m_LayoutInstance;
        private CapsuleSystem m_CapsuleSystem;
        private SpawnSystem m_SpawnSystem;
        private SpawnPacingSystem m_Pacing;
        private RunScoreSystem m_Score;
        private GameplayFeedback m_Feedback;
        private VfxSystem m_Vfx;
        private LevelCameraFramer m_CameraFramer;
        private bool m_RunStarted;
        private bool m_ShutDown;

        public LevelSystem Level { get; private set; }
        public TargetSystem Targets { get; private set; }
        public SpecialPowerSystem Powers { get; private set; }
        public LevelDefinitionSO Definition { get; private set; }
        public bool IsLoaded { get; private set; }

        public GameplaySession(GameContext context)
        {
            m_Context = context;
            m_Assets = context.Services.Get<IAssetProvider>();
        }

        public async UniTask<bool> LoadAsync(int levelIndex)
        {
            CancellationToken token = m_Cts.Token;
            ISaveService save = m_Context.Services.Get<ISaveService>();

            UIPanels.SetLoadingProgress(0.8f);
            TargetCatalogSO targetCatalog = await m_Assets.LoadAsset<TargetCatalogSO>(AddressableKeys.TargetCatalog, token);
            if (token.IsCancellationRequested)
                return false;

            if (targetCatalog == null)
                return Abort("Target catalog missing.");

            UIPanels.SetLoadingProgress(0.85f);
            if (m_Context.LevelCatalog == null || !m_Context.LevelCatalog.TryGetDefinitionKey(levelIndex, out string definitionKey))
                return Abort("Level definition key missing for index " + levelIndex + ".");

            LevelDefinitionSO definition = await m_Assets.LoadAsset<LevelDefinitionSO>(definitionKey, token);
            if (token.IsCancellationRequested)
                return false;

            if (definition == null)
                return Abort("Level definition missing for index " + levelIndex + ".");

            UIPanels.SetLoadingProgress(0.9f);
            if (!definition.TryGetLayoutKey(out string layoutKey)
                && (m_Context.LevelCatalog == null || !m_Context.LevelCatalog.TryGetDefaultLayoutKey(out layoutKey)))
                return Abort("Level layout missing for index " + levelIndex + ".");

            m_LayoutInstance = await m_Assets.Instantiate(layoutKey, null, token);
            if (token.IsCancellationRequested)
                return false;

            if (m_LayoutInstance == null)
                return Abort("Level layout failed to instantiate for index " + levelIndex + ".");

            LevelLayout layout = m_LayoutInstance.GetComponent<LevelLayout>();
            if (layout == null || layout.Paths.Count == 0)
                return Abort("Level prefab is missing LevelLayout or paths.");

            Camera camera = Camera.main;
            if (camera == null || camera.transform.parent == null)
                return Abort("Scene needs a main camera under a camera rig.");

            if (m_Context.GlobalSettings == null)
                return Abort("Global settings missing.");

            IInputService input = m_Context.Services.Get<IInputService>();
            IAudioService audio = m_Context.Services.Get<IAudioService>();
            IGameObjectPool pool = m_Context.Services.Get<IGameObjectPool>();
            audio.PlayMusic(EAudioName.MusicGameplay);

            FeedbackSettingsSO feedbackSettings = await m_Assets.LoadAsset<FeedbackSettingsSO>(AddressableKeys.FeedbackSettings, token);
            if (token.IsCancellationRequested)
                return false;

            if (feedbackSettings == null)
                return Abort("Feedback settings missing.");

            Transform shakeTarget = layout.ShakeHolder != null ? layout.ShakeHolder : camera.transform;
            VfxCatalogSO vfxCatalog = await m_Assets.LoadAsset<VfxCatalogSO>(AddressableKeys.VfxCatalog, token);
            if (token.IsCancellationRequested)
                return false;

            if (vfxCatalog == null)
                Logger.Warning("VFX catalog missing. Gameplay runs without VFX.");

            m_Vfx = new VfxSystem(pool, vfxCatalog != null ? vfxCatalog.Entries : Array.Empty<VfxEntry>(), token);
            m_Feedback = new GameplayFeedback(m_Vfx, audio, shakeTarget, feedbackSettings);
            CapsuleFactory factory = new CapsuleFactory(pool, layout.CapsuleRoot);
            TargetFactory targetFactory = new TargetFactory(pool, targetCatalog);
            Targets = new TargetSystem(targetFactory);
            m_CameraFramer = new LevelCameraFramer(camera, layout, definition, m_Context.GlobalSettings, targetCatalog);
            m_Context.GameLoop.Register(m_CameraFramer);
            UIPanels.SetLoadingProgress(0.95f);
            await Targets.Bind(definition, layout, token);
            if (token.IsCancellationRequested)
                return false;

            await pool.Warmup(AddressableKeys.CapsulePrefab, CapsuleWarmupCount(definition, targetCatalog), token);
            if (token.IsCancellationRequested)
                return false;

            await m_Vfx.Warmup();
            if (token.IsCancellationRequested)
                return false;

            m_CapsuleSystem = new CapsuleSystem(
                input,
                Targets,
                camera,
                token,
                m_Feedback,
                layout.CapsuleMask);
            m_SpawnSystem = new SpawnSystem(factory, m_CapsuleSystem);
            m_Score = new RunScoreSystem(save);
            Level = new LevelSystem(m_Score, save, levelIndex, m_Feedback);
            m_Pacing = new SpawnPacingSystem(m_SpawnSystem, m_CapsuleSystem, layout.Paths, Level, token);
            m_CapsuleSystem.Bind(m_SpawnSystem, Level);

            SpecialPowerCatalogSO powerCatalog = await m_Assets.LoadAsset<SpecialPowerCatalogSO>(AddressableKeys.SpecialPowerCatalog, token);
            if (token.IsCancellationRequested)
                return false;

            Powers = new SpecialPowerSystem(powerCatalog, save, Level, m_Pacing);
            Powers.SyncUnlockGrants();
            Definition = definition;

            m_Context.GameLoop.Register(m_CapsuleSystem);
            m_Context.GameLoop.Register(Level);
            m_Context.GameLoop.Register(m_Pacing);
            m_Context.GameLoop.Register(Powers);
            m_Context.GameLoop.Register(m_Vfx);
            UIPanels.SetLoadingProgress(1f);
            IsLoaded = true;
            return true;
        }

        public void BeginRun()
        {
            if (!IsLoaded)
                return;

            Targets.PublishFill();
            if (m_RunStarted)
                return;

            m_RunStarted = true;
            Level.StartRun(Definition);
        }

        public void Pause()
        {
            Level?.Pause();
        }

        public void Resume()
        {
            Level?.Resume();
        }

        public void Shutdown()
        {
            if (m_ShutDown)
                return;

            m_ShutDown = true;
            IsLoaded = false;
            m_Cts.Cancel();
            m_Cts.Dispose();

            if (Powers != null)
            {
                m_Context.GameLoop.Unregister(Powers);
                Powers.Shutdown();
                Powers = null;
            }

            if (m_CameraFramer != null)
            {
                m_Context.GameLoop.Unregister(m_CameraFramer);
                m_CameraFramer = null;
            }

            if (m_Pacing != null)
            {
                m_Context.GameLoop.Unregister(m_Pacing);
                m_Pacing.Shutdown();
                m_Pacing = null;
            }

            if (m_SpawnSystem != null)
            {
                m_SpawnSystem.DespawnSeated(Targets);
                m_SpawnSystem.DespawnAll();
                m_SpawnSystem = null;
            }

            if (m_CapsuleSystem != null)
            {
                m_Context.GameLoop.Unregister(m_CapsuleSystem);
                m_CapsuleSystem.Shutdown();
                m_CapsuleSystem = null;
            }

            if (Targets != null)
            {
                Targets.Shutdown();
                Targets = null;
            }

            if (Level != null)
            {
                m_Context.GameLoop.Unregister(Level);
                Level.Shutdown();
                Level = null;
            }

            if (m_Score != null)
            {
                m_Score.Shutdown();
                m_Score = null;
            }

            if (m_Feedback != null)
            {
                m_Feedback.Shutdown();
                m_Feedback = null;
            }

            if (m_Vfx != null)
            {
                m_Context.GameLoop.Unregister(m_Vfx);
                m_Vfx.Shutdown();
                m_Vfx = null;
            }

            ReleaseLayout();
            Definition = null;
        }

        private static int CapsuleWarmupCount(LevelDefinitionSO definition, TargetCatalogSO targetCatalog)
        {
            int largestBox = 0;
            foreach (TargetQuota quota in definition.TargetQueue)
                largestBox = Mathf.Max(largestBox, (int)quota.Capacity);

            return definition.MaxActive + targetCatalog.VisibleCount * largestBox + 2;
        }

        private bool Abort(string reason)
        {
            Logger.Error(reason);
            UIPanels.HideLoading();
            return false;
        }

        private void ReleaseLayout()
        {
            if (m_LayoutInstance == null)
                return;

            m_Assets.ReleaseInstance(m_LayoutInstance);
            m_LayoutInstance = null;
        }
    }
}
