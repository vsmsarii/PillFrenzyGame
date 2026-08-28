using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Core
{
    public interface ISceneLoadingUi : IService
    {
        UniTask ShowAsync(CancellationToken cancellationToken = default);
        void SetProgress(float progress);
        void Hide();
    }
}
