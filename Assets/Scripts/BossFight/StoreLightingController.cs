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
            // The fixture's emissive (glowing) renderers, dimmed with the light so a dead light doesn't still glow.
            public List<Renderer> glow = new List<Renderer>();
            public List<Color> glowBase = new List<Color>();
        }

        // Shiny surfaces (the wall mirror) that kept glowing white in the blackout.
        private class Shine
        {
            public Renderer renderer;
            public float smoothness;
            public Color baseColor;
        }

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private const float FixtureGlowRadius = 1.5f;
        private readonly List<Shine> shines = new List<Shine>();
        private MaterialPropertyBlock block;

        private readonly List<Entry> entries = new List<Entry>();
        private BossFightSettings settings;
        private LightTierRules rules;
        private MoneyManager money;
        private float fade = 1f;
        private BlackoutRules blackout;
        private EnvironmentLight originalEnvironment;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            rules = new LightTierRules(settings);
            blackout = new BlackoutRules(settings);
            block = new MaterialPropertyBlock();
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
            originalEnvironment = ReadEnvironment();
            ApplyOutsideLight();
        }

        private void OnDestroy()
        {
            WriteEnvironment(originalEnvironment);
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
            var lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            BossFight.Rules.ShadowRules.RestrictToFlashlight(lights);
            foreach (var light in PrepareStoreLights(lights, outsideLight, transform))
            {
                entries.Add(new Entry
                {
                    light = light,
                    baseIntensity = light.intensity,
                    flicker = rules.CreateFlicker(seed++ * 7919),
                });
            }
            CollectGlowAndShine();
        }

        // Emissive renderers within reach of a store light belong to that light's fixture; mirrors dim with the store.
        private void CollectGlowAndShine()
        {
            shines.Clear();
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (renderer is ParticleSystemRenderer || renderer.GetComponentInParent<Camera>() != null) continue;
                if (renderer.GetComponentInParent<PlayerMovement>() != null || renderer.GetComponentInParent<IBossMinion>() != null) continue;
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                if (renderer.name.ToLowerInvariant().Contains("mirror") || renderer.transform.root.name.ToLowerInvariant().Contains("mirror"))
                {
                    shines.Add(new Shine
                    {
                        renderer = renderer,
                        smoothness = material.HasProperty(SmoothnessId) ? material.GetFloat(SmoothnessId) : 0.5f,
                        baseColor = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white,
                    });
                    continue;
                }
                if (!material.IsKeywordEnabled("_EMISSION") || !material.HasProperty(EmissionColorId)) continue;
                var emission = material.GetColor(EmissionColorId);
                if (emission.maxColorComponent < 0.05f) continue;
                var owner = NearestEntry(renderer.bounds.center);
                if (owner == null) continue;
                owner.glow.Add(renderer);
                owner.glowBase.Add(emission);
            }
        }

        private Entry NearestEntry(Vector3 point)
        {
            Entry best = null;
            float bestDistance = FixtureGlowRadius * FixtureGlowRadius;
            foreach (var e in entries)
            {
                if (e.light == null) continue;
                float d = (e.light.transform.position - point).sqrMagnitude;
                if (d <= bestDistance) { bestDistance = d; best = e; }
            }
            return best;
        }

        // A ceiling piece fell here: lights (and their fixtures) inside the hole go dark for good instead of floating.
        public void RemoveLightsNear(Vector3 point, float radius)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (e.light == null) { entries.RemoveAt(i); continue; }
                var p = e.light.transform.position;
                if ((p.x - point.x) * (p.x - point.x) + (p.z - point.z) * (p.z - point.z) > radius * radius) continue;
                e.light.enabled = false;
                foreach (var r in e.glow) if (r != null) r.enabled = false;
                // Non-emissive fixture parts beside this light (the parent may group every fixture, so stay local).
                var scope = e.light.transform.parent != null ? e.light.transform.parent : e.light.transform;
                foreach (var r in scope.GetComponentsInChildren<Renderer>())
                {
                    if ((r.bounds.center - p).sqrMagnitude <= FixtureGlowRadius * FixtureGlowRadius) r.enabled = false;
                }
                entries.RemoveAt(i);
            }
        }

        // The store ceiling lights among candidates, with their shadows switched off (18 realtime shadow maps
        // were the main frame cost). Lights that are not store lights, the flashlight above all, keep theirs.
        public static List<Light> PrepareStoreLights(IEnumerable<Light> candidates, Light outsideLight, Transform rig)
        {
            var result = new List<Light>();
            foreach (var light in candidates)
            {
                if (light == null || !IsStoreCeilingLight(light, outsideLight, rig)) continue;
                light.shadows = LightShadows.None;
                result.Add(light);
            }
            return result;
        }

        // Excludes the outside light, directional lights, and anything carried by the player's camera or flashlight.
        public static bool IsStoreCeilingLight(Light light, Light outsideLight, Transform rig)
        {
            if (light == outsideLight || light.type == LightType.Directional) return false;
            if (light.GetComponentInParent<PlayerFlashlight>() != null) return false;
            if (light.GetComponentInParent<Camera>() != null) return false;
            if (light.GetComponentInParent<Canvas>() != null) return false;
            if (rig != null && light.transform.IsChildOf(rig)) return false;
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
                for (int i = 0; i < e.glow.Count; i++)
                {
                    var r = e.glow[i];
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor(EmissionColorId, e.glowBase[i] * (flicker * fade));
                    r.SetPropertyBlock(block);
                }
            }
            foreach (var s in shines)
            {
                if (s.renderer == null) continue;
                s.renderer.GetPropertyBlock(block);
                block.SetFloat(SmoothnessId, s.smoothness * fade);
                block.SetColor(BaseColorId, s.baseColor * Mathf.Lerp(0.15f, 1f, fade));
                s.renderer.SetPropertyBlock(block);
            }
            ApplyOutsideLight();
            WriteEnvironment(blackout.Target(originalEnvironment, fade));
        }

        private static EnvironmentLight ReadEnvironment() => new EnvironmentLight
        {
            ambientIntensity = RenderSettings.ambientIntensity,
            ambientLight = RenderSettings.ambientLight,
            ambientSky = RenderSettings.ambientSkyColor,
            ambientEquator = RenderSettings.ambientEquatorColor,
            ambientGround = RenderSettings.ambientGroundColor,
            reflectionIntensity = RenderSettings.reflectionIntensity,
            fogColor = RenderSettings.fogColor,
        };

        private static void WriteEnvironment(EnvironmentLight e)
        {
            RenderSettings.ambientIntensity = e.ambientIntensity;
            RenderSettings.ambientLight = e.ambientLight;
            RenderSettings.ambientSkyColor = e.ambientSky;
            RenderSettings.ambientEquatorColor = e.ambientEquator;
            RenderSettings.ambientGroundColor = e.ambientGround;
            RenderSettings.reflectionIntensity = e.reflectionIntensity;
            RenderSettings.fogColor = e.fogColor;
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
