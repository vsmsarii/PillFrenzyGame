using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PillFrenzy.Core
{
    public interface IGameObjectPool : IService
    {
        UniTask<GameObject> Get(string key, Transform parent = null, CancellationToken cancellationToken = default);
        UniTask<T> Get<T>(string key, Transform parent = null, CancellationToken cancellationToken = default) where T : Component;
        UniTask Warmup(string key, int count, CancellationToken cancellationToken = default);
        void Release(GameObject instance);
    }
}
