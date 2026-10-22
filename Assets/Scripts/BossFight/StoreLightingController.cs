using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // Money-driven store lighting: ceiling lights flicker by tier and fade out at Off, while a dim blue
    // outside light keeps the shop from going pitch black. Lives on the BossFightRig prefab.
    public class StoreLightingController : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Light outsideLight;
        [SerializeField]
        private Color outsideColor = new Color(0.35f, 0.5f, 1f);

        // Tutorial sets this to hold the store at full brightness regardless of money.
        public bool ForceNormal { get; set; }

        public LightTier Tier { get; private set; }

        private class Entry
        {
            public Light light;
            public float baseIntensity;
            public LightFlicker flicker;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private BossFightSettings settings;
        private LightTierRules rules;
        private MoneyManager money;
        private float fade = 1f;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            rules = new LightTierRules(settings);
        }

        private void Start()
        {
            money = FindFirstObjectByType<MoneyManager>();
            CollectLights();
            if (money != null)
            {
                money.OnMoneyChanged += OnMoneyChanged;
                Tier = rules.TierFor(money.CurrentMoney);
            }
            ApplyOutsideLight();
        }

        private void OnDestroy()
        {
            if (money != null) money.OnMoneyChanged -= OnMoneyChanged;
        }

        private void OnMoneyChanged(float amount)
        {
            Tier = rules.TierFor(amount);
        }

        private void CollectLights()
        {
            entries.Clear();
            int seed = 17;
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID))
            {
                if (!IsStoreCeilingLight(light)) continue;
                entries.Add(new Entry
                {
                    light = light,
                    baseIntensity = light.intensity,
                    flicker = rules.CreateFlicker(seed++ * 7919),
                });
            }
        }

        // Excludes the outside light, directional lights, and anything carried by the player's camera or flashlight.
        private bool IsStoreCeilingLight(Light light)
        {
            if (light == outsideLight || light.type == LightType.Directional) return false;
            if (light.GetComponentInParent<PlayerFlashlight>() != null) return false;
            if (light.GetComponentInParent<Camera>() != null) return false;
            if (light.GetComponentInParent<Canvas>() != null) return false;
            if (light.transform.IsChildOf(transform)) return false;
            return true;
        }

        private LightTier EffectiveTier => ForceNormal ? LightTier.Normal : Tier;

        private void Update()
        {
            var tier = EffectiveTier;
            float target = tier == LightTier.Off ? 0f : 1f;
            float step = settings.blackoutFadeSeconds <= 0f ? 1f : Time.deltaTime / settings.blackoutFadeSeconds;
            fade = Mathf.MoveTowards(fade, target, step);

            float now = Time.time;
            foreach (var e in entries)
            {
                if (e.light == null) continue;
                float flicker = e.flicker.Sample(tier, now, settings.flickerIntervalMinSeconds, settings.flickerIntervalMaxSeconds);
                e.light.intensity = e.baseIntensity * flicker * fade;
                e.light.enabled = e.light.intensity > 0.001f;
            }
            ApplyOutsideLight();
        }

        private void ApplyOutsideLight()
        {
            if (outsideLight == null) return;
            outsideLight.color = outsideColor;
            // Rises as the ceiling lights fade, so the blackout is never pitch black.
            outsideLight.intensity = settings.outsideLightIntensity * (1f - fade);
            outsideLight.enabled = fade < 1f;
        }
    }
}
