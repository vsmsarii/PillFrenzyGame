using System;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private TMP_Text m_Price;
        [SerializeField] private Button m_BuyButton;

        private string m_ProductKey;
        private Func<string, UniTask<bool>> m_Purchase;

        private void Awake()
        {
            m_BuyButton.onClick.AddListener(OnBuyClicked);
        }

        public void Bind(IAPCatalogEntry entry, Func<string, UniTask<bool>> purchase)
        {
            m_ProductKey = entry.Key;
            m_Purchase = purchase;

            m_Icon.sprite = entry.Icon;
            m_Icon.enabled = entry.Icon != null;
            m_Title.text = entry.DisplayName;
            m_Description.text = entry.Description;
            m_Price.text = entry.PriceLabel;
        }

        private void OnBuyClicked()
        {
            BuyAsync().Forget();
        }

        private async UniTaskVoid BuyAsync()
        {
            m_BuyButton.interactable = false;
            await m_Purchase.Invoke(m_ProductKey);
            m_BuyButton.interactable = true;
        }
    }
}
