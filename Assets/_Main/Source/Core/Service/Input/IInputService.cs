using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PillFrenzy.Core
{
    public interface IInputService : IService
    {
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        bool TryConsumeTap(out Vector2 screenPosition);
    }
}
