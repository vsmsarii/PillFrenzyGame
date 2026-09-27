using PillFrenzy.Core;

namespace PillFrenzy.Gameplay
{
    public static class SpecialPowerUnlockSync
    {
        public static void Sync(ISaveService save, SpecialPowerCatalogSO catalog, int reachedLevelNumber)
        {
            foreach (SpecialPowerCatalogEntry entry in catalog.Entries)
            {
                if (entry.Definition != null && reachedLevelNumber >= entry.UnlockLevel)
                    save.TryGrantInitialSpecialPower(entry.Definition.Id, entry.InitialCharges);
            }
        }
    }
}
