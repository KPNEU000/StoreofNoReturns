using UnityEngine;

namespace BossFight
{
    // The single home for boss-fight tuning numbers (Assets/Settings/BossFight/BossFightConfig.asset).
    [CreateAssetMenu(menuName = "Boss Fight/Config", fileName = "BossFightConfig")]
    public class BossFightConfig : ScriptableObject
    {
        [SerializeField]
        private BossFightSettings settings = new BossFightSettings();

        // Returns a copy so rule classes can never write back into the asset.
        public BossFightSettings ToSettings() => settings.Clone();

        // An unwired config falls back to the default tuning.
        public static BossFightSettings SettingsOf(BossFightConfig config) => config != null ? config.ToSettings() : new BossFightSettings();
    }
}
