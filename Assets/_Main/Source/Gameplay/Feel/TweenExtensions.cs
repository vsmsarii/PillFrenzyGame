using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace PillFrenzy.Gameplay
{
    public static class TweenExtensions
    {
        public static async UniTask WaitForEnd(this Tween tween, CancellationToken cancellationToken)
        {
            UniTaskCompletionSource ended = new UniTaskCompletionSource();
            tween.OnKill(() => ended.TrySetResult());

            using (cancellationToken.Register(() => tween.Kill()))
                await ended.Task;
        }
    }
}
