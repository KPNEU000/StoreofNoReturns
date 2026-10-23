using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossFight
{
    // A shop clothing rack the Witch can possess. Round 6: it lifts into the air, shakes inside a purple aura (the
    // telegraph), then slams down, bursts, and releases a ring of its own costumes before vanishing.
    // The older Possess (fade, then release at its centre) is kept for anything still calling it.
    public class PossessableRack : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Color possessedColor = new Color(0.6f, 0.1f, 0.9f, 1f);
        // Each costume type hanging on this rack (a rack group can hold several) and its baked cloth garment,
        // index-matched (GarmentClothBuilder); empty on racks without costumes.
        [SerializeField]
        private GameObject[] costumePrefabs = new GameObject[0];
        [SerializeField]
        private GameObject[] garmentPrefabs = new GameObject[0];

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private bool possessed;
        private Vector3 releasePoint;

        public bool IsPossessed => possessed;
        public GameObject[] CostumePrefabs => costumePrefabs;
        public GameObject[] Garments => garmentPrefabs;

        // First garment of the closest rack that has one (the tutorial costume comes from a real rack too).
        public static GameObject NearestGarment(Vector3 position)
        {
            GameObject best = null;
            float bestDistance = float.MaxValue;
            foreach (var rack in FindObjectsByType<PossessableRack>(FindObjectsSortMode.None))
            {
                if (rack.garmentPrefabs == null || rack.garmentPrefabs.Length == 0 || rack.garmentPrefabs[0] == null) continue;
                float d = (rack.transform.position - position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = rack.garmentPrefabs[0];
                }
            }
            return best;
        }
        // Where costumes appear; stays valid after the frame is gone so the spot can be re-used.
        public Vector3 ReleasePoint => possessed ? releasePoint : CenterPoint();

        private Vector3 CenterPoint()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, transform.position.y, bounds.center.z);
        }

        // Returns false when already possessed. onFaded runs once the frame is gone (NavMesh can be rebuilt then).
        public bool Possess(ClothingSpawner spawner, int count, System.Action onFaded = null)
        {
            if (possessed) return false;
            possessed = true;
            releasePoint = CenterPoint();
            StartCoroutine(FadeRoutine(spawner, count, onFaded));
            return true;
        }

        private IEnumerator FadeRoutine(ClothingSpawner spawner, int count, System.Action onFaded)
        {
            var settings = BossFightConfig.SettingsOf(config);
            float seconds = Mathf.Max(settings.rackFadeSeconds, 0.01f);
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            var startScale = transform.localScale;
            var burst = CreateBurst();
            burst.transform.position = releasePoint + Vector3.up * 0.8f;
            burst.Play();
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / seconds);
                var color = Color.Lerp(Color.white, possessedColor, Mathf.Min(1f, k * 2f));
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(block);
                    block.SetColor(BaseColorId, color);
                    block.SetColor(ColorId, color);
                    block.SetColor(EmissionColorId, possessedColor * k);
                    r.SetPropertyBlock(block);
                }
                transform.localScale = startScale * Mathf.Lerp(1f, 0.05f, k * k);
                yield return null;
            }
            foreach (var r in renderers)
            {
                r.SetPropertyBlock(null);
                r.enabled = false;
            }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            transform.localScale = startScale;
            onFaded?.Invoke();
            if (spawner != null) spawner.Spawn(releasePoint, count, garmentPrefabs);
            Destroy(burst.gameObject, 3f);
        }

        private static readonly Color TelegraphGlow = new Color(1f, 0.95f, 0.9f, 1f);
        private static Material glowMaterial;
        private float liftMeters;

        private static Bounds RendererBounds(Component c)
        {
            var renderers = c.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(c.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // Clear of every other rack, under the ceiling.
        private static float LiftFor(PossessableRack rack)
        {
            var own = RendererBounds(rack);
            float neighbourTop = own.max.y;
            foreach (var other in FindObjectsByType<PossessableRack>(FindObjectsSortMode.None))
            {
                if (other == rack || other.possessed) continue;
                neighbourTop = Mathf.Max(neighbourTop, RendererBounds(other).max.y);
            }
            var ceiling = GameObject.Find("Ceiling");
            var ceilingRenderer = ceiling != null ? ceiling.GetComponent<Renderer>() : null;
            float ceilingBottom = ceilingRenderer != null ? ceilingRenderer.bounds.min.y : own.max.y + 4f;
            return Rules.RackLift.Meters(own.min.y, own.size.y, neighbourTop, ceilingBottom);
        }

        // The rack's own materials have no emission, so a property-block glow never showed: swap to a solid,
        // over-bright unlit material for the telegraph (the rack disappears on release, so nothing to restore).
        private static Material GlowMaterial()
        {
            if (glowMaterial != null) return glowMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return null;
            glowMaterial = new Material(shader) { name = "RackTelegraphGlow" };
            glowMaterial.SetColor("_BaseColor", new Color(1.6f, 1.5f, 1.35f, 1f));
            return glowMaterial;
        }
        private Coroutine telegraph;
        private ParticleSystem aura;
        private Light auraLight;
        private Vector3 restPosition;

        // Starts the lift-and-shake telegraph. Returns false when already possessed.
        public bool BeginTelegraph(float seconds)
        {
            if (possessed) return false;
            possessed = true;
            releasePoint = CenterPoint();
            restPosition = transform.position;
            liftMeters = LiftFor(this);
            telegraph = StartCoroutine(TelegraphRoutine(seconds));
            return true;
        }

        private IEnumerator TelegraphRoutine(float seconds)
        {
            var settings = BossFightConfig.SettingsOf(config);
            var renderers = GetComponentsInChildren<Renderer>();
            aura = CreateAura();
            aura.transform.position = releasePoint + Vector3.up * 1f;
            auraLight = new GameObject("RackAuraLight").AddComponent<Light>();
            auraLight.type = LightType.Point;
            // Playtest round 7: the rack must read from across the store, so it glows bright white (the purple stays
            // in the particle aura) and lights its surroundings.
            auraLight.color = TelegraphGlow;
            auraLight.range = 9f;
            auraLight.shadows = LightShadows.None;
            auraLight.transform.position = releasePoint + Vector3.up * 1.2f;
            seconds = Mathf.Max(seconds, 0.01f);
            var glowSwap = GlowMaterial();
            bool swapped = false;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / seconds);
                // Lights up within the first fifth of the telegraph and stays lit.
                if (!swapped && glowSwap != null && k >= 0.2f)
                {
                    swapped = true;
                    foreach (var r in renderers)
                    {
                        if (r == null) continue;
                        var lit = new Material[r.sharedMaterials.Length];
                        for (int i = 0; i < lit.Length; i++) lit[i] = glowSwap;
                        r.sharedMaterials = lit;
                    }
                }
                // Rises quickly, then hangs and shakes harder and harder.
                float lift = liftMeters * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, k * 2.5f));
                var shake = Random.insideUnitSphere * (0.03f + 0.12f * k);
                shake.y *= 0.4f;
                transform.position = restPosition + Vector3.up * lift + shake;
                auraLight.intensity = Mathf.Lerp(6f, 18f, k) * (0.85f + 0.15f * Mathf.Sin(t * 30f));
                yield return null;
            }
            telegraph = null;
        }

        // Slams the rack down, bursts and releases the wave around it, then the rack is gone. onGone runs once its
        // colliders are off (the NavMesh can be updated then). Returns the costumes that were spawned.
        public List<PossessedClothing> Release(ClothingSpawner spawner, int count, float hpMultiplier, System.Action onGone = null)
        {
            var settings = BossFightConfig.SettingsOf(config);
            if (telegraph != null) StopCoroutine(telegraph);
            telegraph = null;
            transform.position = restPosition;
            var burst = CreateBurst();
            burst.transform.position = releasePoint + Vector3.up * 0.8f;
            var main = burst.main;
            main.startSpeed = 5f;
            var emission = burst.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 160) });
            burst.Play();
            Destroy(burst.gameObject, 3f);
            if (aura != null) Destroy(aura.gameObject);
            if (auraLight != null) Destroy(auraLight.gameObject, 0.15f);
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.SetPropertyBlock(null);
                r.enabled = false;
            }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            onGone?.Invoke();
            return spawner != null
                ? spawner.SpawnRing(releasePoint, count, garmentPrefabs, settings.waveRingMinRadius, settings.waveRingMaxRadius, hpMultiplier)
                : new List<PossessedClothing>();
        }

        private ParticleSystem CreateAura()
        {
            var go = new GameObject("RackAura");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startColor = possessedColor;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.gravityModifier = -0.35f;
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 160f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.2f, 1.6f, 1.2f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
            ps.Play();
            return ps;
        }

        // Procedural purple particle burst; no particle assets needed.
        private ParticleSystem CreateBurst()
        {
            var go = new GameObject("RackPossessBurst");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startColor = possessedColor;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.startSize = 0.15f;
            main.gravityModifier = -0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
            return ps;
        }
    }
}
