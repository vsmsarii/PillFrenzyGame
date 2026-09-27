using System.Collections.Generic;
using PillFrenzy.Core;

namespace PillFrenzy.Gameplay
{
    public sealed class TutorialSelector
    {
        private readonly TutorialCatalogSO m_Catalog;
        private readonly ISaveService m_Save;
        private readonly SpecialPowerSystem m_Powers;

        public TutorialSelector(TutorialCatalogSO catalog, ISaveService save, SpecialPowerSystem powers)
        {
            m_Catalog = catalog;
            m_Save = save;
            m_Powers = powers;
        }

        public void CollectPending(int levelIndex, LevelDefinitionSO definition, List<TutorialDefinitionSO> buffer)
        {
            buffer.Clear();
            foreach (TutorialDefinitionSO tutorial in m_Catalog.Tutorials)
            {
                if (tutorial == null || !tutorial.HasContent || m_Save.HasSeenTutorial(tutorial.Id))
                    continue;

                if (Matches(tutorial, levelIndex + 1, definition))
                    InsertByOrder(buffer, tutorial);
            }
        }

        public void CollectPending(int levelIndex, LevelDefinitionSO definition, bool beforeRun, List<TutorialDefinitionSO> buffer)
        {
            CollectPending(levelIndex, definition, buffer);
            buffer.RemoveAll(tutorial => tutorial.IsBeforeRun != beforeRun);
        }

        private bool Matches(TutorialDefinitionSO tutorial, int levelNumber, LevelDefinitionSO definition)
        {
            switch (tutorial.Condition)
            {
                case ETutorialCondition.LevelNumber:
                    return levelNumber == tutorial.LevelNumber;
                case ETutorialCondition.CapsuleKindAvailable:
                    return levelNumber == tutorial.LevelNumber && IsCapsuleKindAvailable(definition, tutorial.CapsuleKind);
                case ETutorialCondition.SpecialPowerUnlocked:
                    return IsSpecialPowerUnlockLevel(tutorial.SpecialPower, levelNumber);
                default:
                    return false;
            }
        }

        private bool IsSpecialPowerUnlockLevel(ESpecialPowerId id, int levelNumber)
        {
            return m_Powers.Catalog.TryGet(id, out SpecialPowerCatalogEntry entry)
                && levelNumber == entry.UnlockLevel
                && m_Powers.IsUnlocked(id);
        }

        private static bool IsCapsuleKindAvailable(LevelDefinitionSO definition, ECapsuleKind kind)
        {
            switch (kind)
            {
                case ECapsuleKind.Normal:
                    return definition.NormalDefinition != null;
                case ECapsuleKind.Gold:
                    return definition.GoldDefinition != null && definition.GoldChance > 0f;
                case ECapsuleKind.Poison:
                    return definition.PoisonDefinition != null && definition.PoisonChance > 0f;
                default:
                    return false;
            }
        }

        private static void InsertByOrder(List<TutorialDefinitionSO> buffer, TutorialDefinitionSO tutorial)
        {
            int index = buffer.Count;
            while (index > 0 && buffer[index - 1].Order > tutorial.Order)
                index--;

            buffer.Insert(index, tutorial);
        }
    }
}
