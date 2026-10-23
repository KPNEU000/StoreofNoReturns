using System;
using System.Collections;
using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // A placeholder book on a Boss Scene shelf. The Witch's attack claims it, it glows and hangs, flies in a
    // straight line (through enemies), then tumbles, loses its glow and settles into inert, kinematic clutter.
    public class ThrowableBook : MonoBehaviour
    {
        public enum BookState
        {
            Shelved,
            Claimed,
            Flying,
            Tumbling,
            Inert
        }

        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Material trailMaterial;
        [SerializeField]
        private Color glowColor = new Color(0.7f, 0.2f, 1f, 1f);
        [SerializeField]
        private float glowIntensity = 3f;
        [SerializeField]
        private float lightIntensity = 2.5f;
        [SerializeField]
        private float lightRange = 4f;
        [SerializeField]
        private float spinDegreesPerSecond = 540f;
        [SerializeField]
        private float settleMaxSeconds = 6f;
        [SerializeField]
        private float settleSpeed = 0.1f;
        [SerializeField]
        private AudioClip whooshSound;
        [SerializeField]
        private AudioClip impactSound;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Renderer bookRenderer;
        // The page block and its cover plates/spine; they all glow together.
        private MeshRenderer[] glowRenderers;
        private Collider bookCollider;
        private Light glowLight;
        private TrailRenderer trail;
        private ParticleSystem sparks;
        private const float SparkRate = 90f;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Transform homeParent;
        private Coroutine routine;

        public BookState State { get; private set; }
        public bool IsShelved => State == BookState.Shelved;
        public bool IsInert => State == BookState.Inert;
        public float InertSince { get; private set; }

        private void Awake()
        {
            bookRenderer = GetComponentInChildren<Renderer>();
            glowRenderers = GetComponentsInChildren<MeshRenderer>(true);
            bookCollider = GetComponent<Collider>();
            EnemyContact.IgnorePlayer(gameObject);
            BookThrowRules.ScaleAboutBase(transform.localPosition, transform.localScale, BossFightConfig.SettingsOf(config).bookScale,
                out var scaledPosition, out var scaledScale);
            transform.localPosition = scaledPosition;
            transform.localScale = scaledScale;
            homeParent = transform.parent;
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;

            foreach (var r in glowRenderers) r.material.EnableKeyword("_EMISSION");
            SetGlow(0f);
            var lightObject = new GameObject("BookGlow");
            lightObject.transform.SetParent(transform, false);
            glowLight = lightObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glowColor;
            glowLight.range = lightRange;
            glowLight.shadows = LightShadows.None;
            glowLight.intensity = 0f;
            glowLight.enabled = false;

            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.widthMultiplier = 0.2f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.sharedMaterial = trailMaterial != null ? trailMaterial : new Material(Shader.Find("Sprites/Default"));
            trail.startColor = glowColor;
            trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            trail.emitting = false;
            sparks = MakeSparks();
        }

        public void Claim()
        {
            State = BookState.Claimed;
        }

        // The telegraph: the claimed book lights up purple.
        public void Glow()
        {
            SetGlow(1f);
        }

        // Back on its shelf slot, dark and ready to be picked again.
        public void ResetToShelf()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            var body = GetComponent<Rigidbody>();
            if (body != null) Destroy(body);
            transform.SetParent(homeParent, false);
            transform.localPosition = homePosition;
            transform.localRotation = homeRotation;
            if (bookCollider != null) bookCollider.isTrigger = false;
            trail.emitting = false;
            trail.Clear();
            SetGlow(0f);
            State = BookState.Shelved;
        }

        // Flies at `target` (fixed now, not homing). `canHit` is asked on each overlap with the player.
        public void Launch(Vector3 target, BookThrowRules rules, PlayerHitReceiver receiver, Func<Vector3> playerCenter, Func<bool> canHit)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FlyRoutine(target, rules, receiver, playerCenter, canHit));
        }

        private IEnumerator FlyRoutine(Vector3 target, BookThrowRules rules, PlayerHitReceiver receiver, Func<Vector3> playerCenter, Func<bool> canHit)
        {
            State = BookState.Flying;
            transform.SetParent(null, true);
            if (bookCollider != null) bookCollider.isTrigger = true;
            trail.Clear();
            trail.emitting = true;
            if (whooshSound != null) AudioSource.PlayClipAtPoint(whooshSound, transform.position);

            var direction = BookThrowRules.FlightDirection(transform.position, target);
            bool hit = false;
            while (true)
            {
                transform.position = rules.StepFlight(transform.position, direction, Time.deltaTime);
                transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
                if (receiver != null && canHit() && rules.Hits(transform.position, playerCenter()))
                {
                    receiver.TryHit(HitKind.Book);
                    hit = true;
                    break;
                }
                if (BookThrowRules.ReachedTarget(transform.position, direction, target)) break;
                yield return null;
            }
            if (impactSound != null) AudioSource.PlayClipAtPoint(impactSound, transform.position);
            trail.emitting = false;
            routine = StartCoroutine(TumbleRoutine(rules, hit ? -direction * 2f : direction * 3f, receiver));
        }

        // Falls and bounces as a rigid body that ignores the player and every agent, fades its glow, then
        // freezes into a trigger: inert floor clutter that cannot be pushed or block anyone.
        private IEnumerator TumbleRoutine(BookThrowRules rules, Vector3 velocity, PlayerHitReceiver receiver)
        {
            State = BookState.Tumbling;
            if (bookCollider != null)
            {
                bookCollider.isTrigger = false;
                IgnoreColliders(receiver != null ? receiver.GetComponentsInChildren<Collider>() : null);
                foreach (var agent in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
                {
                    IgnoreColliders(agent.GetComponentsInChildren<Collider>());
                }
            }
            var body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.mass = 0.5f;
            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;

            float start = Time.time;
            float stillSince = -1f;
            while (Time.time - start < settleMaxSeconds)
            {
                float t = Time.time - start;
                float glow = rules.GlowAt(t);
                SetGlow(glow);
                bool slow = body.linearVelocity.magnitude < settleSpeed && body.angularVelocity.magnitude < settleSpeed * 4f;
                if (t > 0.3f && slow)
                {
                    if (stillSince < 0f) stillSince = Time.time;
                }
                else
                {
                    stillSince = -1f;
                }
                if (glow <= 0f && stillSince >= 0f && Time.time - stillSince > 0.3f) break;
                yield return null;
            }
            SetGlow(0f);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            if (bookCollider != null) bookCollider.isTrigger = true;
            InertSince = Time.time;
            State = BookState.Inert;
            routine = null;
        }

        private void IgnoreColliders(Collider[] others)
        {
            if (others == null) return;
            foreach (var other in others)
            {
                if (other != null && other != bookCollider) Physics.IgnoreCollision(bookCollider, other, true);
            }
        }

        // 0 = dark book, 1 = full purple glow (emissive plus point light).
        // Purple sparks shed while the book is charged (telegraph and flight), scaled with its glow.
        private ParticleSystem MakeSparks()
        {
            var go = new GameObject("BookSparks");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.7f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = glowColor;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            ParticleLook.Apply(ps);
            ps.Play();
            return ps;
        }

        private void SetGlow(float amount)
        {
            if (sparks != null)
            {
                var emission = sparks.emission;
                emission.rateOverTime = SparkRate * amount;
            }
            if (glowRenderers != null)
            {
                foreach (var r in glowRenderers)
                {
                    if (r != null) r.material.SetColor(EmissionColorId, glowColor * (glowIntensity * amount));
                }
            }
            if (glowLight != null)
            {
                glowLight.intensity = lightIntensity * amount;
                glowLight.enabled = amount > 0.001f;
            }
        }
    }
}
