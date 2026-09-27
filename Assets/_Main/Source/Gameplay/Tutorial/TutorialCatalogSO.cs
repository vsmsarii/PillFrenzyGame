using System.Collections.Generic;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(menuName = "PillFrenzy/Tutorial Catalog", fileName = "TutorialCatalog")]
    public sealed class TutorialCatalogSO : ScriptableObject
    {
        [SerializeField] private TutorialDefinitionSO[] m_Tutorials;

        public IReadOnlyList<TutorialDefinitionSO> Tutorials => m_Tutorials;

        private void OnValidate()
        {
            if (m_Tutorials == null)
                return;

            HashSet<string> ids = new HashSet<string>();
            foreach (TutorialDefinitionSO tutorial in m_Tutorials)
            {
                if (tutorial == null || string.IsNullOrEmpty(tutorial.Id))
                    continue;

                if (!ids.Add(tutorial.Id))
                    Logger.Warning("Duplicate tutorial id in catalog: " + tutorial.Id, this);
            }
        }
    }
}
