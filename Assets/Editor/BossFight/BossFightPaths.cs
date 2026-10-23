namespace BossFight.Editor
{
    public static class BossFightPaths
    {
        public const string SettingsFolder = "Assets/Settings/BossFight";
        public const string PrefabsFolder = "Assets/Prefabs/BossFight";
        public const string ModelsFolder = "Assets/Models/BossFight";
        public const string ConfigAsset = SettingsFolder + "/BossFightConfig.asset";
        public const string RigPrefab = PrefabsFolder + "/BossFightRig.prefab";
        public const string RigName = "BossFightRig";

        public const string ScenesFolder = "Assets/Scenes/Scenes in Game";
        public const string BossScene = ScenesFolder + "/Boss Scene.unity";
        public const string MainScene = ScenesFolder + "/MainScene.unity";

        // Build order; "You WIn Screen" really has a capital I in its filename.
        public static readonly string[] BuildScenes =
        {
            ScenesFolder + "/Title Screen.unity",
            MainScene,
            BossScene,
            ScenesFolder + "/Game Over Screen.unity",
            ScenesFolder + "/You WIn Screen.unity",
            ScenesFolder + "/Just Credits.unity", // Game Over's CREDITS button loads it by name
        };
    }
}
