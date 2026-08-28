namespace PillFrenzy.Core
{
    public static class AddressableKeys
    {
        public const string CapsulePrefab = "prefab.capsule";
        public const string InputActions = "input.actions";
        public const string ColorTable = "def.color.table";
        public const string TargetCatalog = "def.target.catalog";
        public const string SpecialPowerCatalog = "def.special.power.catalog";
        public const string IapCatalog = "def.iap.catalog";
        public const string GlobalSettings = "def.global.settings";
        public const string LevelManifest = "def.level.manifest";
        public const string FeedbackSettings = "def.feedback.settings";
        public const string AudioCatalog = "def.audio.catalog";
        public const string DefSpecialPowerSlow = "def.special.power.slow";

        public const string DefCapsuleGold = "def.capsule.gold";
        public const string DefCapsulePoison = "def.capsule.poison";

        public const string VfxPop = "vfx.pop";
        public const string VfxGold = "vfx.gold";
        public const string VfxPoison = "vfx.poison";

        public const string SfxCorrect = "sfx.correct";
        public const string SfxGold = "sfx.gold";
        public const string SfxPoison = "sfx.poison";
        public const string SfxComplete = "sfx.complete";
        public const string SfxFail = "sfx.fail";

        public const string UiPanelCatalog = "ui.panel.catalog";
        public const string UiMainMenu = "ui.menu";
        public const string UiGameplay = "ui.gameplay";
        public const string UiWin = "ui.win";
        public const string UiLose = "ui.lose";
        public const string UiLoading = "ui.loading";
        public const string UiShop = "ui.shop";
        public const string UiSettings = "ui.settings";

        public static string PrefabTarget(int capacity)
        {
            return "prefab.target." + capacity.ToString();
        }

        public static string DefCapsuleNormal(string colorId)
        {
            return $"def.capsule.normal.{colorId}";
        }

        public static string DefLevel(int levelIndex)
        {
            return "def.level." + (levelIndex + 1).ToString();
        }
    }
}
