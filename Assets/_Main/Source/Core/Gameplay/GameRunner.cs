using UnityEngine;

namespace PillFrenzy.Core
{
    public sealed class GameRunner : MonoBehaviour
    {
        public static GameRunner Instance { get; private set; }

        public GameContext Context { get; private set; }

        public void Bind(GameContext context)
        {
            Context = context;
            Instance = this;
        }

        private void Update()
        {
            Context.GameLoop.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            Context.GameLoop.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            Context.GameLoop.LateTick(Time.deltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Context.Services.Get<ISaveService>().FlushPending();

            EB.Presentation.Invoke(new ApplicationPauseChanged(paused));
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            Context?.Dispose();
            Context = null;
        }
    }
}
