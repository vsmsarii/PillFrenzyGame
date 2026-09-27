using System;
using System.Collections.Generic;

namespace PillFrenzy.Core
{
    public sealed class ServiceProvider
    {
        private readonly Dictionary<Type, IService> m_Services = new();
        private readonly List<IService> m_RegistrationOrder = new();
        private readonly GameLoop m_GameLoop;

        public ServiceProvider(GameLoop gameLoop)
        {
            m_GameLoop = gameLoop;
        }

        public void Register<T>(T service) where T : class, IService
        {
            if (m_RegistrationOrder.Contains(service))
            {
                Logger.Warning("Service already registered: " + service.GetType().Name);
                return;
            }

            service.Initialize();
            m_RegistrationOrder.Add(service);
            m_GameLoop.Register(service);
            m_Services[typeof(T)] = service;

            foreach (Type interfaceType in service.GetType().GetInterfaces())
            {
                if (interfaceType != typeof(IService) && typeof(IService).IsAssignableFrom(interfaceType))
                    m_Services[interfaceType] = service;
            }
        }

        public T Get<T>() where T : class, IService
        {
            return (T)m_Services[typeof(T)];
        }

        public void Dispose()
        {
            for (int i = m_RegistrationOrder.Count - 1; i >= 0; i--)
            {
                IService service = m_RegistrationOrder[i];
                m_GameLoop.Unregister(service);
                service.Dispose();
            }

            m_RegistrationOrder.Clear();
            m_Services.Clear();
        }
    }
}
