using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // A possessed costume: hovers, paths on the runtime NavMesh, lunges at the player, then flops.
    [RequireComponent(typeof(NavMeshAgent))]
    public class PossessedClothing : MonoBehaviour, IDamageable, IBossMinion
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private GameObject costumePrefab;
        [SerializeField]
        private float hoverHeight = 1f;
        [SerializeField]
        private float bobAmplitude = 0.1f;
        [SerializeField]
        private float contactDistance = 1.2f;
        [SerializeField]
        private float lungeSpeedMultiplier = 2.5f;
        [SerializeField]
        private Color possessedTint = new Color(0.45f, 0.1f, 0.8f);
        [SerializeField]
        private float damageFlashSeconds = 0.15f;

        private NavMeshAgent agent;
        private ClothingBrain brain;
        private FlankRouting routing;
        private Flank flank;
        private ClothingSpawner spawner;
        private Transform visual;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private PlayerHitReceiver receiver;
        private Transform player;
        private float chaseSpeed;
        private float bobPhase;
        private float flashUntil;
        private bool tinted;
        private bool lifeless;
        private bool harmless;
        private bool patrolToB;
        private Vector3 patrolA;
        private Vector3 patrolB;

        public bool IsAlive => !lifeless && brain != null && brain.IsAlive;
        public ClothingState State => brain != null ? brain.State : ClothingState.Dead;

        public bool Harmless => harmless;

        public void Init(ClothingSpawner owner, int index)
        {
            spawner = owner;
            flank = FlankRouting.Assign(index);
        }

        // Tutorial costume: never hits the player, floats back and forth between two points instead of chasing.
        public void MakeHarmless(Vector3 from, Vector3 to)
        {
            harmless = true;
            patrolA = from;
            patrolB = to;
            patrolToB = true;
        }

        private void Awake()
        {
            var settings = BossFightConfig.SettingsOf(config);
            brain = new ClothingBrain(settings);
            routing = new FlankRouting(settings);
            agent = GetComponent<NavMeshAgent>();
            bobPhase = Random.value * Mathf.PI * 2f;

            var movement = FindFirstObjectByType<PlayerMovement>();
            float walk = movement != null ? movement.speed : 10f;
            chaseSpeed = walk * settings.clothingSpeedFactor;
            agent.speed = chaseSpeed;
            agent.baseOffset = hoverHeight;

            if (costumePrefab != null)
            {
                visual = Instantiate(costumePrefab, transform).transform;
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                foreach (var collider in visual.GetComponentsInChildren<Collider>())
                {
                    Destroy(collider);
                }
            }
            else
            {
                visual = transform;
            }
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            ApplyTint(possessedTint);

            // Hit detection (sword OverlapSphere) needs a collider; the agent drives movement.
            if (GetComponent<Collider>() == null)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.zero; // the agent's baseOffset already lifts the transform to hover height
                capsule.height = 1.6f;
                capsule.radius = 0.4f;
            }
        }

        private void Start()
        {
            player = movementTransform();
            receiver = FindFirstObjectByType<PlayerHitReceiver>();
            // The rig's NavMeshSurface builds in its Awake; place the agent on the mesh before it moves.
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        private Transform movementTransform()
        {
            var movement = FindFirstObjectByType<PlayerMovement>();
            return movement != null ? movement.transform : null;
        }

        private void Update()
        {
            if (!brain.IsAlive) return;
            var before = brain.State;
            brain.Tick(Time.deltaTime);
            if (brain.State != before) OnStateChanged(before, brain.State);

            if (visual != null && visual != transform)
            {
                var pos = visual.localPosition;
                pos.y = brain.State == ClothingState.Flopped ? -hoverHeight + 0.15f : Mathf.Sin(Time.time * 2f + bobPhase) * bobAmplitude;
                visual.localPosition = pos;
            }
            if (tinted && Time.time >= flashUntil) ApplyTint(possessedTint);

            if (player == null || !agent.isOnNavMesh) return;
            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            if (harmless)
            {
                var goal = patrolToB ? patrolB : patrolA;
                var toGoal = goal - transform.position;
                toGoal.y = 0f;
                if (toGoal.magnitude < 0.8f) patrolToB = !patrolToB;
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.SetDestination(goal);
                return;
            }

            switch (brain.State)
            {
                case ClothingState.Chase:
                    agent.SetDestination(routing.Destination(player.position, transform.position, flank));
                    if (toPlayer.magnitude <= contactDistance) brain.OnContact();
                    if (brain.State == ClothingState.Lunge) OnStateChanged(ClothingState.Chase, ClothingState.Lunge);
                    break;
                case ClothingState.Lunge:
                    agent.SetDestination(player.position);
                    if (receiver != null && TutorialRules.CanHurtPlayer(harmless) && toPlayer.magnitude <= contactDistance * 1.25f && brain.CanDealHit)
                    {
                        receiver.TryHit(HitKind.Clothing);
                    }
                    break;
            }
        }

        private void OnStateChanged(ClothingState from, ClothingState to)
        {
            switch (to)
            {
                case ClothingState.Lunge:
                    agent.isStopped = false;
                    agent.speed = chaseSpeed * lungeSpeedMultiplier;
                    break;
                case ClothingState.Flopped:
                    agent.isStopped = true;
                    agent.ResetPath();
                    SetVisualLying(true);
                    break;
                case ClothingState.Rising:
                    SetVisualLying(false);
                    break;
                case ClothingState.Chase:
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    break;
            }
        }

        private void SetVisualLying(bool lying)
        {
            if (visual != null && visual != transform)
            {
                visual.localRotation = lying ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            }
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (!IsAlive) return;
            brain.TakeDamage(amount);
            if (!brain.IsAlive)
            {
                Die();
                return;
            }
            flashUntil = Time.time + damageFlashSeconds;
            ApplyTint(Color.red);
        }

        // The Witch died: stop and fall wherever it is.
        public void DropLifeless()
        {
            if (!IsAlive) return;
            lifeless = true;
            Die();
        }

        private void Die()
        {
            if (spawner != null) spawner.Unregister(this);
            agent.enabled = false;
            enabled = false;
            if (EnemyDeathRules.OutcomeFor(EnemyKind.PossessedClothing) == DeathOutcome.Vanish) SpawnPuff();
            Destroy(gameObject);
        }

        // A short purple burst where the garment was; it cleans itself up.
        private void SpawnPuff()
        {
            var puff = new GameObject("ClothingPuff");
            puff.transform.position = transform.position;
            var particles = puff.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.7f;
            main.startSpeed = 2f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.55f, 0.15f, 0.9f, 0.8f);
            main.gravityModifier = -0.1f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            var renderer = puff.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var material = new Material(shader);
                material.SetColor("_BaseColor", main.startColor.color);
                renderer.sharedMaterial = material;
            }
            particles.Play();
        }

        private void ApplyTint(Color color)
        {
            tinted = color != possessedTint;
            RendererTint.Apply(renderers, block, color, color * 1.5f);
        }

        private void OnDestroy()
        {
            if (spawner != null) spawner.Unregister(this);
        }
    }
}
