using System;
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
            for (int i = m_Content.childCount - 1; i >= 0; i--)
                Destroy(m_Content.GetChild(i).gameObject);

            foreach (IAPCatalogEntry entry in m_Catalog.Entries)
            {
                if (IsVisible(entry))
                    Instantiate(m_ItemPrefab, m_Content).Bind(entry, key => m_Iap.PurchaseAsync(key));
            }
        }

        private bool IsVisible(IAPCatalogEntry entry)
        {
            if (entry.RewardType != EIAPRewardType.SpecialPowerCharges)
                return true;

            return m_Powers.TryGet(entry.PowerId, out SpecialPowerCatalogEntry power)
                && m_Save.CurrentLevelNumber >= power.UnlockLevel;
        }

        private void OnCloseClicked()
        {
            m_Close.Invoke();
        }
    }
}
