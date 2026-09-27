using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PillFrenzy.Core
{
    public sealed class SceneService : Service, ISceneService
    {
        private const float SceneProgressWeight = 0.7f;
        private const float UnityLoadCompleteProgress = 0.9f;

        private readonly ISceneLoadingUi m_Loading;

        public SceneService(ISceneLoadingUi loading)
        {
            m_Loading = loading;
        }

        public async UniTask Load(ESceneName scene, CancellationToken cancellationToken = default)
        {
            await m_Loading.ShowAsync(cancellationToken);

            AsyncOperation operation = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single);
            while (!operation.isDone)
            {
                float sceneProgress = Mathf.Clamp01(operation.progress / UnityLoadCompleteProgress);
                m_Loading.SetProgress(sceneProgress * SceneProgressWeight);
                await UniTask.Yield(cancellationToken);
            }

            m_Loading.SetProgress(SceneProgressWeight);
        }
    }
}
