using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Gameplay
{
    public sealed class PreRunSequence
    {
        private readonly List<IPreRunStep> m_Steps = new();

        public void Add(IPreRunStep step)
        {
            m_Steps.Add(step);
        }

        public void Clear()
        {
            m_Steps.Clear();
        }

        public async UniTask RunAsync(PreRunContext context, CancellationToken cancellationToken)
        {
            foreach (IPreRunStep step in m_Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (step.ShouldRun(context))
                    await step.RunAsync(context, cancellationToken);
            }
        }
    }
}
