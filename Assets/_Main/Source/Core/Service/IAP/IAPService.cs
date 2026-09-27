using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Purchasing;

namespace PillFrenzy.Core
{
    public sealed class IAPService : Service, IIAPService
    {
        private static readonly TimeSpan ProductFetchTimeout = TimeSpan.FromSeconds(8);

        private readonly IAssetProvider m_Assets;
        private readonly ISaveService m_Save;

        private IAPCatalogSO m_Catalog;
        private StoreController m_Store;
        private UniTaskCompletionSource<bool> m_PurchaseTcs;
        private UniTaskCompletionSource<bool> m_FetchTcs;
        private bool m_ProductsReady;
        private bool m_StoreAvailable;

        public IAPService(IAssetProvider assets, ISaveService save)
        {
            m_Assets = assets;
            m_Save = save;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            m_Catalog = await m_Assets.LoadAsset<IAPCatalogSO>(AddressableKeys.IapCatalog, cancellationToken);

#if !UNITY_EDITOR
            await ConnectStoreAsync(cancellationToken);
#endif
        }

        public async UniTask<bool> PurchaseAsync(string productKey, CancellationToken cancellationToken = default)
        {
            if (!m_Catalog.TryGet(productKey, out IAPCatalogEntry entry))
            {
                Logger.Error("IAP product missing: " + productKey);
                return false;
            }

            if (!m_StoreAvailable || !m_ProductsReady)
            {
#if UNITY_EDITOR
                ApplyReward(entry);
                return true;
#else
                Logger.Error("IAP store not ready for: " + productKey);
                return false;
#endif
            }

            if (m_PurchaseTcs != null)
                return false;

            UniTaskCompletionSource<bool> purchaseTcs = new UniTaskCompletionSource<bool>();
            m_PurchaseTcs = purchaseTcs;
            using (cancellationToken.Register(() => AbandonPurchase(purchaseTcs)))
            {
                m_Store.PurchaseProduct(productKey);
                return await purchaseTcs.Task;
            }
        }

        private async UniTask ConnectStoreAsync(CancellationToken cancellationToken)
        {
            m_Store = UnityIAPServices.StoreController();
            m_Store.OnPurchasePending += OnPurchasePending;
            m_Store.OnPurchaseFailed += OnPurchaseFailed;
            m_Store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_Store.OnProductsFetched += OnProductsFetched;
            m_Store.OnProductsFetchFailed += OnProductsFetchFailed;
            m_Store.OnStoreDisconnected += OnStoreDisconnected;

            await m_Store.Connect();
            if (cancellationToken.IsCancellationRequested)
                return;

            m_StoreAvailable = true;
            m_FetchTcs = new UniTaskCompletionSource<bool>();
            UniTask<bool> fetched = m_FetchTcs.Task;
            FetchCatalogProducts();

            (bool timedOut, bool success) = await fetched.TimeoutWithoutException(ProductFetchTimeout);
            if (!timedOut)
                m_ProductsReady = success;
        }

        private void FetchCatalogProducts()
        {
            CatalogProvider catalogProvider = new CatalogProvider();
            foreach (IAPCatalogEntry entry in m_Catalog.Entries)
                catalogProvider.AddProduct(entry.Key, ProductType.Consumable, CreateStoreIds(entry));

            catalogProvider.FetchProducts(m_Store.FetchProductsWithNoRetries);
        }

        private static StoreSpecificIds CreateStoreIds(IAPCatalogEntry entry)
        {
            bool hasGooglePlayId = !string.IsNullOrEmpty(entry.GooglePlayId);
            bool hasAppStoreId = !string.IsNullOrEmpty(entry.AppStoreId);
            if (!hasGooglePlayId && !hasAppStoreId)
                return null;

            StoreSpecificIds ids = new StoreSpecificIds();
            if (hasGooglePlayId)
                ids.Add(entry.GooglePlayId, GooglePlay.Name);
            if (hasAppStoreId)
                ids.Add(entry.AppStoreId, AppleAppStore.Name);

            return ids;
        }

        private void OnProductsFetched(List<Product> products)
        {
            m_ProductsReady = products.Count > 0;
            CompleteFetch(m_ProductsReady);
        }

        private void OnProductsFetchFailed(ProductFetchFailed _)
        {
            m_ProductsReady = false;
            CompleteFetch(false);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription _)
        {
            m_StoreAvailable = false;
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string productKey = order.CartOrdered.Items()[0].Product.definition.id;
            if (!m_Catalog.TryGet(productKey, out IAPCatalogEntry entry))
            {
                Logger.Error("IAP pending order for unknown product left unconfirmed: " + productKey);
                CompletePurchase(false);
                return;
            }

            ApplyReward(entry);
            m_Store.ConfirmPurchase(order);
            CompletePurchase(true);
        }

        private void OnPurchaseFailed(FailedOrder _)
        {
            CompletePurchase(false);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder)
                CompletePurchase(false);
        }

        private void CompleteFetch(bool success)
        {
            UniTaskCompletionSource<bool> tcs = m_FetchTcs;
            m_FetchTcs = null;
            tcs?.TrySetResult(success);
        }

        private void CompletePurchase(bool success)
        {
            UniTaskCompletionSource<bool> tcs = m_PurchaseTcs;
            m_PurchaseTcs = null;
            tcs?.TrySetResult(success);
        }

        private void AbandonPurchase(UniTaskCompletionSource<bool> tcs)
        {
            if (m_PurchaseTcs == tcs)
                m_PurchaseTcs = null;

            tcs.TrySetResult(false);
        }

        private void ApplyReward(IAPCatalogEntry entry)
        {
            switch (entry.RewardType)
            {
                case EIAPRewardType.ImmortalityMinutes:
                    m_Save.GrantImmortalityMinutes(entry.RewardAmount);
                    break;
                case EIAPRewardType.SpecialPowerCharges:
                    m_Save.AddSpecialPowerCharges(entry.PowerId, entry.RewardAmount);
                    break;
                case EIAPRewardType.Hearts:
                    m_Save.GrantHearts(entry.RewardAmount);
                    break;
            }

            m_Save.FlushPending();
        }

        protected override void OnDispose()
        {
            CompleteFetch(false);
            CompletePurchase(false);
            if (m_Store == null)
                return;

            m_Store.OnPurchasePending -= OnPurchasePending;
            m_Store.OnPurchaseFailed -= OnPurchaseFailed;
            m_Store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            m_Store.OnProductsFetched -= OnProductsFetched;
            m_Store.OnProductsFetchFailed -= OnProductsFetchFailed;
            m_Store.OnStoreDisconnected -= OnStoreDisconnected;
            m_Store = null;
        }
    }
}
