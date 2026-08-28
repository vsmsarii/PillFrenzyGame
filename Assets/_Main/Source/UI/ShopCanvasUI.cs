using System;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class ShopCanvasUI : MonoBehaviour
    {
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Transform m_Content;
        [SerializeField] private ShopItemView m_ItemPrefab;

        private IIAPService m_Iap;
        private IAPCatalogSO m_Catalog;
        private ISaveService m_Save;
        private SpecialPowerCatalogSO m_Powers;
        private Action m_Close;

        private void Awake()
        {
            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(OnCloseClicked);
        }

        public void Bind(
            IAPCatalogSO catalog,
            IIAPService iap,
            ISaveService save,
            SpecialPowerCatalogSO powers,
            Action close)
        {
            m_Catalog = catalog;
            m_Iap = iap;
            m_Save = save;
            m_Powers = powers;
            m_Close = close;
            Rebuild();
        }

        private void Rebuild()
        {
            if (m_Content == null)
                return;

            if (m_ItemPrefab == null)
                return;

            for (int i = m_Content.childCount - 1; i >= 0; i--)
                Destroy(m_Content.GetChild(i).gameObject);

            if (m_Catalog == null || m_Catalog.Entries == null)
                return;

            for (int i = 0; i < m_Catalog.Entries.Length; i++)
            {
                IAPCatalogEntry entry = m_Catalog.Entries[i];
                if (string.IsNullOrEmpty(entry.Key) || !IsVisible(entry))
                    continue;

                ShopItemView item = Instantiate(m_ItemPrefab, m_Content);
                item.Bind(entry, PurchaseAsync);
            }
        }

        private bool IsVisible(IAPCatalogEntry entry)
        {
            if (entry.RewardType != EIAPRewardType.SpecialPowerCharges)
                return true;

            if (m_Powers == null || m_Save == null)
                return false;

            if (!m_Powers.TryGet(entry.PowerId, out SpecialPowerCatalogEntry power))
                return false;

            return m_Save.CurrentLevelNumber >= power.UnlockLevel;
        }

        private async UniTask<bool> PurchaseAsync(string key)
        {
            if (m_Iap == null)
                return false;

            return await m_Iap.PurchaseAsync(key);
        }

        private void OnCloseClicked()
        {
            if (m_Close != null)
                m_Close.Invoke();
            else
                UIPanels.Close(EUIPanel.Shop);
        }
    }
}
