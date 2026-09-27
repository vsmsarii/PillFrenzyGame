namespace PillFrenzy.Core
{
    public static class AddressableKeys
    {
        public const string CapsulePrefab = "prefab.capsule";
        public const string InputActions = "input.actions";
        public const string TargetCatalog = "def.target.catalog";
        public const string SpecialPowerCatalog = "def.special.power.catalog";
        public const string IapCatalog = "def.iap.catalog";
        public const string GlobalSettings = "def.global.settings";
        public const string LevelManifest = "def.level.manifest";
        public const string FeedbackSettings = "def.feedback.settings";
        public const string AudioCatalog = "def.audio.catalog";
        public const string VfxCatalog = "def.vfx.catalog";
        public const string TutorialCatalog = "def.tutorial.catalog";
        public const string AdSettings = "def.ad.settings";
        public const string UiPanelCatalog = "ui.panel.catalog";
        public const string DummyAdView = "ui.dummy.ad";

        public static string DefLevel(int levelIndex)
        {
            return "def.level." + (levelIndex + 1);
        }
    }
}
