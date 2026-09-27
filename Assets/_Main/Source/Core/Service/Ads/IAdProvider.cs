using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Core
{
    public interface IAdProvider
    {
        UniTask InitializeAsync(bool testAds, CancellationToken cancellationToken);
        UniTask<EAdResult> ShowAsync(EAdType type, string adUnitId, CancellationToken cancellationToken);
    }
}
