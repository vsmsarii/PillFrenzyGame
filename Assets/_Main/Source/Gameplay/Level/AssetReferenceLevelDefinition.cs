using System;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public sealed class AssetReferenceLevelDefinition : AssetReferenceT<LevelDefinitionSO>
    {
        public AssetReferenceLevelDefinition(string guid) : base(guid)
        {
        }
    }
}
