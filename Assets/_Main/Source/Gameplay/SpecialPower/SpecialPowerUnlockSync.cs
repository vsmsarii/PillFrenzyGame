using PillFrenzy.Core;

namespace PillFrenzy.Gameplay
{
    public static class SpecialPowerUnlockSync
    {
        public static void Sync(ISaveService save, SpecialPowerCatalogSO catalog)
        {
            if (save == null || catalog == null || catalog.Entries == null)
                return;

            int levelNumber = save.CurrentLevelNumber;
            SpecialPowerCatalogEntry[] entries = catalog.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                SpecialPowerCatalogEntry catalogEntry = entries[i];
                SpecialPowerDefinitionSO definition = catalogEntry.Definition;
                if (definition == null || definition.Id == ESpecialPowerId.None)
                    continue;

                if (levelNumber < catalogEntry.UnlockLevel)
                    continue;

                save.TryGrantInitialSpecialPower(definition.Id, catalogEntry.InitialCharges);
            }
        }
    }
}
