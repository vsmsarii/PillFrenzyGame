using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Core
{
    public interface IAdService : IService
    {
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        bool IsDueAfterLevel(int levelIndex);
        UniTask<EAdResult> ShowAsync(int levelIndex, CancellationToken cancellationToken = default);
    }
}
