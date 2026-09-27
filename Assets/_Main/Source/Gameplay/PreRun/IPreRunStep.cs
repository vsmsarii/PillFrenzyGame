using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Gameplay
{
    public interface IPreRunStep
    {
        bool ShouldRun(PreRunContext context);
        UniTask RunAsync(PreRunContext context, CancellationToken cancellationToken);
    }
}
