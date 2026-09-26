using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public sealed class BootInit : MonoBehaviour
    {
        private GameContext m_Context;

        private void Awake()
        {
            if (GameRunner.Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Logger.SetMinimumLevel(ELogLevel.Warning);
#endif

            m_Context = AppInstaller.CreateContext(gameObject);
        }

        private void Start()
        {
            if (m_Context == null)
                return;

            BootAsync().Forget();
        }

        private async UniTaskVoid BootAsync()
        {
            await AppInstaller.LoadGlobalsAsync(m_Context);

            ISaveService save = m_Context.Services.Get<ISaveService>();
            ESceneName scene = save.HasCompletedFirstLevel ? ESceneName.Menu : ESceneName.Gameplay;
            await m_Context.Services.Get<ISceneService>().Load(scene, m_Context.CancellationToken);
        }
    }
}
