using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PillFrenzy.Core
{
    public sealed class AssetProvider : Service, IAssetProvider
    {
        private readonly Dictionary<string, LoadedAsset> m_Assets = new();
        private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> m_InstanceHandles = new();

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            AsyncOperationHandle handle = Addressables.InitializeAsync();
            await handle.Task.AsUniTask().AttachExternalCancellation(cancellationToken);
        }

        public async UniTask<T> LoadAsset<T>(string key, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (m_Assets.TryGetValue(key, out LoadedAsset loaded))
                return loaded.Retain() as T;

            AsyncOperationHandle<T> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                (bool canceled, T asset) = await handle.Task.AsUniTask().AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();
                if (canceled || asset == null)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    return null;
                }

                if (m_Assets.TryGetValue(key, out loaded))
                {
                    Addressables.Release(handle);
                    return loaded.Retain() as T;
                }

                m_Assets[key] = new LoadedAsset(handle);
                return asset;
            }
            catch (Exception exception)
            {
                Logger.Error("Asset load failed for key " + key + ": " + exception.Message);
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }
        }

        public void ReleaseAsset(string key)
        {
            if (!m_Assets.TryGetValue(key, out LoadedAsset loaded) || loaded.ReleaseReference() > 0)
                return;

            Addressables.Release(loaded.Handle);
            m_Assets.Remove(key);
        }

        public async UniTask<GameObject> Instantiate(string key, Transform parent = null, CancellationToken cancellationToken = default)
        {
            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                handle = Addressables.InstantiateAsync(key, parent);
                (bool canceled, GameObject instance) = await handle.Task.AsUniTask().AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();
                if (canceled || instance == null)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    return null;
                }

                m_InstanceHandles[instance] = handle;
                return instance;
            }
            catch (Exception exception)
            {
                Logger.Error("Instantiate failed for key " + key + ": " + exception.Message);
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
                return;

            if (m_InstanceHandles.TryGetValue(instance, out AsyncOperationHandle<GameObject> handle))
            {
                m_InstanceHandles.Remove(instance);
                if (handle.IsValid())
                    Addressables.ReleaseInstance(handle);
                return;
            }

            Addressables.ReleaseInstance(instance);
        }

        protected override void OnDispose()
        {
            foreach (AsyncOperationHandle<GameObject> handle in m_InstanceHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.ReleaseInstance(handle);
            }

            m_InstanceHandles.Clear();

            foreach (LoadedAsset loaded in m_Assets.Values)
                Addressables.Release(loaded.Handle);

            m_Assets.Clear();
        }

        private sealed class LoadedAsset
        {
            private int m_References = 1;

            public AsyncOperationHandle Handle { get; }

            public LoadedAsset(AsyncOperationHandle handle)
            {
                Handle = handle;
            }

            public object Retain()
            {
                m_References++;
                return Handle.Result;
            }

            public int ReleaseReference()
            {
                return --m_References;
            }
        }
    }
}
