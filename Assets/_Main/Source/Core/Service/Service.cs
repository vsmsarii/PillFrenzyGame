namespace PillFrenzy.Core
{
    public abstract class Service : IService
    {
        private bool m_Initialized;

        public void Initialize()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;
            OnInitialize();
        }

        public void Dispose()
        {
            if (!m_Initialized)
                return;

            OnDispose();
            m_Initialized = false;
        }

        protected virtual void OnInitialize(){}
        protected virtual void OnDispose(){}
    }
}
