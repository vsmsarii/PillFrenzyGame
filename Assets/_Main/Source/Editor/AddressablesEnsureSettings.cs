using PillFrenzy.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace PillFrenzy.Editor
{
    [InitializeOnLoad]
    public static class AddressablesEnsureSettings
    {
        static AddressablesEnsureSettings()
        {
            EditorApplication.delayCall += Ensure;
        }

        private static void Ensure()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
                return;

            EnsureEntry(settings, "8d3c1a7e5b924f06a9c2e4d1b0f8a375", AddressableKeys.CapsulePrefab);
            EnsureEntry(settings, "e1b5c7d40a8e6123d6f9a4e1c3b57082", AddressableKeys.PrefabTarget(4));
            EnsureEntry(settings, "fe8520959a1c340d181959353c474d9e", AddressableKeys.PrefabTarget(6));
            EnsureEntry(settings, "538885df9d74245acb1bf24b96505108", AddressableKeys.PrefabTarget(8));
            EnsureEntry(settings, "21c4e8a07b3d4f159e6a8c2d4b0f1735", AddressableKeys.DefCapsuleNormal("red"));
            EnsureEntry(settings, "32d5f9b18c4e5026af7b9d3e5c102846", AddressableKeys.DefCapsuleNormal("blue"));
            EnsureEntry(settings, "54f7a1b20c6e7248b19cae5f7d324068", AddressableKeys.DefCapsuleGold);
            EnsureEntry(settings, "65a8b2c31d7f8359c20dbf6e8e435179", AddressableKeys.DefCapsulePoison);
            EnsureEntry(settings, "43e60ac29d5f6137b08cae4f6d213957", AddressableKeys.DefLevel(0));
            EnsureEntry(settings, "5f86aeb02cd4865794e5671819202122", AddressableKeys.DefLevel(1));
            EnsureEntry(settings, "0f28105bd18a90abf44663f14c87329f", AddressableKeys.DefLevel(2));
            EnsureEntry(settings, "0fc95af5ccb276e3658fe6c3f03fa571", AddressableKeys.DefLevel(3));
            EnsureEntry(settings, "493f5bbe1279a6aba81e26df6eefbe98", AddressableKeys.DefLevel(4));
            EnsureEntry(settings, "31249684dfe33e13d3768e174a032537", AddressableKeys.DefLevel(5));
            EnsureEntry(settings, "81ea587d28ad9a29a8992086d0ca0e5f", AddressableKeys.DefLevel(6));
            EnsureEntry(settings, "d201a982ebe3b0c530d4eafe9e94f38d", AddressableKeys.DefLevel(7));
            EnsureEntry(settings, "4c155f17291c0fa1f7b54be4ecdbdd2e", AddressableKeys.DefLevel(8));
            EnsureEntry(settings, "7c8f65a4bd0abb9cca2b545943b43a71", AddressableKeys.DefLevel(9));
            EnsureEntry(settings, "2406f749e98631d2fa3397f8e459f7ea", AddressableKeys.DefLevel(10));
            EnsureEntry(settings, "ded20143c462b49be94425e0692f4520", AddressableKeys.DefLevel(11));
            EnsureEntry(settings, "cbe8c61a593cc621d30f7a84c683e6df", AddressableKeys.DefLevel(12));
            EnsureEntry(settings, "5450629ab517e023628917e70457ebb9", AddressableKeys.DefLevel(13));
            EnsureEntry(settings, "1a9ffd4674b17e5babc54b9d76a38f1d", AddressableKeys.DefLevel(14));
            EnsureEntry(settings, "a9c4e2b17d8f4065b3a0c1e4f5d67890", "layout.level.1");
            EnsureEntry(settings, "6097bfc13de59768a5f6781920212223", "layout.level.2");
            EnsureEntry(settings, "d8e4a1c27b3f4e509c6d8a0b1f2e3456", AddressableKeys.InputActions);
            EnsureEntry(settings, "c7e9f1a23b4d5068e0f1a2b3c4d5e678", AddressableKeys.ColorTable);
            EnsureEntry(settings, "c1d2e3f405162738495a6b7c8d9e0f12", AddressableKeys.TargetCatalog);
            EnsureEntry(settings, "7e8f90123456789abcdef0123456789a", AddressableKeys.DefSpecialPowerSlow);
            EnsureEntry(settings, "8f90123456789abcdef0123456789abc", AddressableKeys.SpecialPowerCatalog);
            EnsureEntry(settings, "90123456789abcdef0123456789abcde", AddressableKeys.IapCatalog);
            EnsureEntry(settings, "a1b2c3d4e5f60718293a4b5c6d7e8f90", AddressableKeys.GlobalSettings);
            EnsureEntryByPath(settings, "Assets/_Main/SO/Level/LevelManifest.asset", AddressableKeys.LevelManifest);
            EnsureEntry(settings, "a0d10ca7a10000000000000000000001", AddressableKeys.AudioCatalog);
            EnsureEntry(settings, "2c537b8d9fa1532461b2341516171819", AddressableKeys.VfxPop);
            EnsureEntry(settings, "3d648c9e0ab2643572c3451617181920", AddressableKeys.VfxGold);
            EnsureEntry(settings, "4e759daf1bc3754683d4561718192021", AddressableKeys.VfxPoison);
            EnsureEntry(settings, "71a8c0d2e4f64789b0c1d2e3f4a5b6c7", AddressableKeys.SfxCorrect);
            EnsureEntry(settings, "82b9d1e3f507589ac1d2e3f4a5b6c7d8", AddressableKeys.SfxGold);
            EnsureEntry(settings, "93cae2f4061869abd2e3f4a5b6c7d8e9", AddressableKeys.SfxPoison);
            EnsureEntry(settings, "a4dbf30517297abce3f4a5b6c7d8e9f0", AddressableKeys.SfxComplete);
            EnsureEntry(settings, "b5ec0416283a8bcdf4a5b6c7d8e9f001", AddressableKeys.SfxFail);
            EnsureEntry(settings, "d9e0f1a23b4c5d6e7f8091a2b3c4d5e6", AddressableKeys.UiPanelCatalog);
            EnsureEntryByPath(settings, "Assets/_Main/Prefab/UI/MainMenuCanvas.prefab", AddressableKeys.UiMainMenu);
            EnsureEntryByPath(settings, "Assets/_Main/Prefab/UI/GameplayCanvas.prefab", AddressableKeys.UiGameplay);
            EnsureEntryByPath(settings, "Assets/_Main/Prefab/UI/WinCanvas.prefab", AddressableKeys.UiWin);
            EnsureEntryByPath(settings, "Assets/_Main/Prefab/UI/LoseCanvas.prefab", AddressableKeys.UiLose);
            EnsureEntry(settings, "f6a7b8c9d0e1425364758697a8b9c0d1", AddressableKeys.UiLoading);
            EnsureEntry(settings, "a0123456789abcdef0123456789abcde", AddressableKeys.UiShop);
            EnsureEntry(settings, "5e771105000000000000000000000001", AddressableKeys.UiSettings);
        }

        private static void EnsureEntryByPath(AddressableAssetSettings settings, string assetPath, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return;

            EnsureEntry(settings, guid, address);
        }

        private static void EnsureEntry(AddressableAssetSettings settings, string guid, string address)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null)
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);

            if (entry != null && entry.address != address)
                entry.SetAddress(address, false);
        }
    }
}
