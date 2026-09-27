using System;
using System.Collections.Generic;

namespace PillFrenzy.Core
{
    public class EBBase
    {
        private readonly Dictionary<Type, Delegate> m_EventTable = new();

        public void Add<T>(Action<T> listener)
        {
            Type key = typeof(T);
            if (m_EventTable.TryGetValue(key, out Delegate existing))
                m_EventTable[key] = (Action<T>)existing + listener;
            else
                m_EventTable[key] = listener;
        }

        public void Remove<T>(Action<T> listener)
        {
            Type key = typeof(T);
            if (!m_EventTable.TryGetValue(key, out Delegate current))
                return;

            Action<T> updated = (Action<T>)current - listener;
            if (updated == null)
                m_EventTable.Remove(key);
            else
                m_EventTable[key] = updated;
        }

        public void Invoke<T>(T evt)
        {
            if (m_EventTable.TryGetValue(typeof(T), out Delegate listeners))
                ((Action<T>)listeners).Invoke(evt);
        }

        public void Clear()
        {
            m_EventTable.Clear();
        }
    }
}
