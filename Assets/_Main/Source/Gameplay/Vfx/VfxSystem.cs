using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class VfxSystem : ITickable
    {
        private readonly IGameObjectPool m_Pool;
        private readonly CancellationToken m_Token;
        private readonly Dictionary<EVfxId, VfxEntry> m_Entries = new();
        private readonly List<PooledVfx> m_Active = new();

        public VfxSystem(IGameObjectPool pool, IReadOnlyList<VfxEntry> entries, CancellationToken token)
        {
            m_Pool = pool;
            m_Token = token;

            for (int i = 0; i < entries.Count; i++)
            {
                VfxEntry entry = entries[i];
                if (!entry.IsValid)
                    continue;

                if (m_Entries.ContainsKey(entry.Id))
                    Logger.Warning("Duplicate VFX entry for " + entry.Id + ". The first one is used.");
                else
                    m_Entries.Add(entry.Id, entry);
            }
        }

        public async UniTask Warmup()
        {
            foreach (VfxEntry entry in m_Entries.Values)
            {
                if (entry.Warmup > 0)
                    await m_Pool.Warmup(Key(entry), entry.Warmup, m_Token);
            }
        }

        public void Play(EVfxId id, Vector3 position, Color tint)
        {
            if (m_Entries.TryGetValue(id, out VfxEntry entry))
                Spawn(entry, position, tint).Forget();
        }

        public void Tick(float deltaTime)
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                PooledVfx vfx = m_Active[i];
                if (vfx.Tick(deltaTime))
                    continue;

                m_Active.RemoveAt(i);
                Release(vfx);
            }
        }

        public void Shutdown()
        {
            for (int i = 0; i < m_Active.Count; i++)
                Release(m_Active[i]);

            m_Active.Clear();
        }

        private async UniTaskVoid Spawn(VfxEntry entry, Vector3 position, Color tint)
        {
            GameObject instance = await m_Pool.Get(Key(entry), null, m_Token);
            if (instance == null)
                return;

            if (m_Token.IsCancellationRequested)
            {
                m_Pool.Release(instance);
                return;
            }

            PooledVfx vfx = instance.GetComponent<PooledVfx>();
            if (vfx == null)
                vfx = instance.AddComponent<PooledVfx>();

            instance.transform.SetPositionAndRotation(position + entry.SpawnOffset, Quaternion.identity);
            vfx.Play(entry.TintWithCapsuleColor ? tint : (Color?)null);
            m_Active.Add(vfx);
        }

        private void Release(PooledVfx vfx)
        {
            vfx.Stop();
            m_Pool.Release(vfx.gameObject);
        }

        private static string Key(VfxEntry entry)
        {
            return entry.Prefab.RuntimeKey.ToString();
        }
    }
}
