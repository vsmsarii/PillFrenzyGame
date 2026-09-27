using System;
using System.Collections.Generic;

namespace PillFrenzy.Core
{
    public sealed class GameLoop
    {
        private readonly Registry<ITickable> m_Tickables = new();
        private readonly Registry<IFixedTickable> m_FixedTickables = new();
        private readonly Registry<ILateTickable> m_LateTickables = new();

        public void Register(object obj)
        {
            if (obj is ITickable tickable)
                m_Tickables.Add(tickable);

            if (obj is IFixedTickable fixedTickable)
                m_FixedTickables.Add(fixedTickable);

            if (obj is ILateTickable lateTickable)
                m_LateTickables.Add(lateTickable);
        }

        public void Unregister(object obj)
        {
            if (obj is ITickable tickable)
                m_Tickables.Remove(tickable);

            if (obj is IFixedTickable fixedTickable)
                m_FixedTickables.Remove(fixedTickable);

            if (obj is ILateTickable lateTickable)
                m_LateTickables.Remove(lateTickable);
        }

        public void Tick(float deltaTime)
        {
            foreach (ITickable tickable in m_Tickables.Snapshot())
            {
                try
                {
                    tickable.Tick(deltaTime);
                }
                catch (Exception exception)
                {
                    LogFailure("Tick", tickable, exception);
                }
            }
        }

        public void FixedTick(float deltaTime)
        {
            foreach (IFixedTickable tickable in m_FixedTickables.Snapshot())
            {
                try
                {
                    tickable.FixedTick(deltaTime);
                }
                catch (Exception exception)
                {
                    LogFailure("FixedTick", tickable, exception);
                }
            }
        }

        public void LateTick(float deltaTime)
        {
            foreach (ILateTickable tickable in m_LateTickables.Snapshot())
            {
                try
                {
                    tickable.LateTick(deltaTime);
                }
                catch (Exception exception)
                {
                    LogFailure("LateTick", tickable, exception);
                }
            }
        }

        private static void LogFailure(string phase, object tickable, Exception exception)
        {
            Logger.Error(phase + " failed in " + tickable.GetType().Name + ": " + exception);
        }

        private sealed class Registry<T>
        {
            private readonly List<T> m_Items = new();
            private T[] m_Snapshot = Array.Empty<T>();
            private bool m_Changed;

            public void Add(T item)
            {
                if (m_Items.Contains(item))
                    return;

                m_Items.Add(item);
                m_Changed = true;
            }

            public void Remove(T item)
            {
                if (m_Items.Remove(item))
                    m_Changed = true;
            }

            public T[] Snapshot()
            {
                if (m_Changed)
                {
                    m_Snapshot = m_Items.ToArray();
                    m_Changed = false;
                }

                return m_Snapshot;
            }
        }
    }
}
