using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class BootMenu : MonoBehaviour
    {
        private const int PopupLayer = 1;

        private GameContext m_Context;
        private ISaveService m_Save;
        private IAudioService m_Audio;
        private IAPCatalogSO m_IapCatalog;
        private SpecialPowerCatalogSO m_PowerCatalog;

        private void Awake()
        {
            if (GameRunner.Instance == null)
            {
                Logger.Error("GameRunner missing. Play from Init.");
                return;
            }

            m_Context = GameRunner.Instance.Context;
            m_Save = m_Context.Services.Get<ISaveService>();
            m_Audio = m_Context.Services.Get<IAudioService>();
            EB.Presentation.Add<UIPanelOpened>(OnPanelOpened);
        }

        private void Start()
        {
            if (m_Context != null)
                RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            IAssetProvider assets = m_Context.Services.Get<IAssetProvider>();
            m_IapCatalog = await assets.LoadAsset<IAPCatalogSO>(AddressableKeys.IapCatalog, m_Context.CancellationToken);
            m_PowerCatalog = await assets.LoadAsset<SpecialPowerCatalogSO>(AddressableKeys.SpecialPowerCatalog, m_Context.CancellationToken);
            SpecialPowerUnlockSync.Sync(m_Save, m_PowerCatalog, m_Save.CurrentLevelNumber);
            m_Audio.PlayMusic(EAudioName.MusicMenu);

            EB.Presentation.Invoke(new OpenUIPanelEvent(EUIPanel.MainMenu, 0));
        }

        private void OnPanelOpened(UIPanelOpened opened)
        {
            switch (opened.Panel)
            {
                case EUIPanel.MainMenu:
                    BindMainMenu(opened.Instance.GetComponent<MainMenuCanvasUI>());
                    break;
                case EUIPanel.Shop:
                    opened.Instance.GetComponent<ShopCanvasUI>().Bind(
                        m_IapCatalog,
                        m_Context.Services.Get<IIAPService>(),
                        m_Save,
                        m_PowerCatalog,
                        () => UIPanels.Close(EUIPanel.Shop));
                    break;
                case EUIPanel.Settings:
                    opened.Instance.GetComponent<SettingsCanvasUI>().Bind(m_Audio, () => UIPanels.Close(EUIPanel.Settings));
                    break;
            }
        }

        private void BindMainMenu(MainMenuCanvasUI menu)
        {
            int levelIndex = Mathf.Min(m_Save.CurrentLevelIndex, m_Context.LevelCatalog.LastLevelIndex);
            menu.Bind(m_Save, levelIndex + 1, () => Play(levelIndex), OpenShop, OpenSettings);
            UIPanels.SetLoadingProgress(1f);
            UIPanels.HideLoading();
        }

        private void OpenShop()
        {
            m_Audio.Play(EAudioName.SfxUiClick);
            EB.Presentation.Invoke(new OpenUIPanelEvent(EUIPanel.Shop, PopupLayer));
        }

        private void OpenSettings()
        {
            m_Audio.Play(EAudioName.SfxUiClick);
            EB.Presentation.Invoke(new OpenUIPanelEvent(EUIPanel.Settings, PopupLayer));
        }

        private void Play(int levelIndex)
        {
            m_Context.GameplayLevelIndex = levelIndex;
            m_Context.Services.Get<ISceneService>().Load(ESceneName.Gameplay, m_Context.CancellationToken).Forget();
        }

        private void OnDestroy()
        {
            EB.Presentation.Remove<UIPanelOpened>(OnPanelOpened);
        }
    }
}
