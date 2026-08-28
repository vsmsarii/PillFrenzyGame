using PillFrenzy.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "LevelManifest", menuName = "PillFrenzy/Level Manifest")]
    public sealed class LevelManifestSO : ScriptableObject, ILevelCatalog
    {
        [SerializeField] private AssetReferenceLevelDefinition[] m_Levels;
        [SerializeField] private AssetReferenceGameObject m_DefaultLayout;

        public int LevelCount => m_Levels != null ? m_Levels.Length : 0;
        public int LastLevelIndex => LevelCount < 1 ? 0 : LevelCount - 1;

        public bool TryGetDefinitionKey(int index, out string key)
        {
            key = null;
            if (m_Levels == null || index < 0 || index >= m_Levels.Length)
                return false;

            AssetReferenceLevelDefinition reference = m_Levels[index];
            if (reference == null || !reference.RuntimeKeyIsValid())
                return false;

            key = reference.RuntimeKey.ToString();
            return true;
        }

        public bool TryGetDefaultLayoutKey(out string key)
        {
            key = null;
            if (m_DefaultLayout == null || !m_DefaultLayout.RuntimeKeyIsValid())
                return false;

            key = m_DefaultLayout.RuntimeKey.ToString();
            return true;
        }
    }
}
