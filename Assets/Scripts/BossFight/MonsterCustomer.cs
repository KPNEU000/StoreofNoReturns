using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // An angered customer: walks the NavMesh toward the player, winds up a swing and hits through PlayerHitReceiver.
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterCustomer : MonoBehaviour, IDamageable, IBossMinion
    {
        private const string RunState = "Run";
        private const string AttackState = "Attack";
        private const string HurtState = "Hurt";

        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private float stoppingDistance = 1.2f;
        [SerializeField]
        private float turnSpeed = 360f;
        [SerializeField]
        private float hurtSeconds = 0.4f;
        [SerializeField]
        private float damageFlashSeconds = 0.15f;
        [SerializeField]
        private float eyeSize = 0.05f;
        [SerializeField]
        private float eyeSpacing = 0.07f;
        [SerializeField]
        private Color eyeColor = new Color(1f, 0.05f, 0.05f);
        [SerializeField]
        private Color boostTint = new Color(0.6f, 0.15f, 1f);
        [SerializeField]
        private AudioClip hurtSound;
        [SerializeField]
        private AudioClip deathSound;

        private NavMeshAgent agent;
        private readonly RepathGate repath = new RepathGate();
        private MonsterAttack attack;
        private FlankRouting routing;
        private ReputationScaling reputationScaling;
        private BossFightSettings settings;
        private BossDirector director;
        private PlayerHitReceiver receiver;
        private Transform player;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private Flank flank;
        private float walkSpeed = 10f;
        private float hp;
        private float spawnDelay;
        private float hurtUntil;
        private float flashUntil;
        private bool tinted;
        private bool active;
        private bool boosted;
        private bool dead;

        public bool IsAlive => !dead;
        public bool Boosted => boosted;
        public float Health => hp;

        // Called by the spawner right after Instantiate. The monster waits delay seconds before it starts moving.
        public void Init(BossDirector owner, int index, float delay, float reputation, string customerName = null)
        {
            director = owner;
            flank = FlankRouting.Assign(index);
            spawnDelay = delay;
            hp = settings.monsterBaseHp * reputationScaling.MonsterHpMultiplier(reputation);
            if (director != null) director.RegisterMonster(this);
            var drag = GetComponent<MonsterDrag>();
            if (drag != null) drag.Begin(this, customerName);
        }

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            attack = new MonsterAttack(settings);
            routing = new FlankRouting(settings);
            reputationScaling = new ReputationScaling(settings);
            hp = settings.monsterBaseHp;
            agent = GetComponent<NavMeshAgent>();
            NavAgents.Configure(agent);
            NavAgents.ExcludeFromNavMesh(gameObject);
            agent.enabled = false;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            renderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => !(r is ParticleSystemRenderer) && !r.name.StartsWith("Eye"));
            block = new MaterialPropertyBlock();
            var movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                player = movement.transform;
                walkSpeed = movement.speed;
            }
            BuildEyes();
        }

        private void Start()
        {
            receiver = FindFirstObjectByType<PlayerHitReceiver>();
        }

        private void Update()
        {
            if (dead) return;
            if (!active)
            {
                spawnDelay -= Time.deltaTime;
                if (spawnDelay <= 0f) Activate();
                return;
            }
            if (tinted && Time.time >= flashUntil) ApplyTint(boosted ? boostTint : Color.white, boosted);
            if (player == null || !agent.isOnNavMesh) return;

            if (Time.time < hurtUntil)
            {
                agent.isStopped = true;
                return;
            }

            var pos = transform.position;
            if (attack.State == MonsterAttackState.Windup)
            {
                agent.isStopped = true;
            }
            else if (attack.TryBegin(pos, player.position))
            {
                agent.isStopped = true;
                Play(AttackState);
            }
            else
            {
                agent.isStopped = false;
                NavAgents.Steer(agent, repath, routing.Destination(player.position, pos, flank));
                if (attack.State == MonsterAttackState.Idle) Play(RunState);
                RefreshSpeed();
            }

            if (attack.Tick(Time.deltaTime, pos, transform.forward, player.position) && receiver != null)
            {
                receiver.TryHit(HitKind.Monster);
            }
            if (attack.State == MonsterAttackState.Cooldown) Play(RunState);
        }

        private void Activate()
        {
            active = true;
            if (NavMesh.SamplePosition(transform.position, out var hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            agent.enabled = true;
            agent.stoppingDistance = stoppingDistance;
            agent.angularSpeed = turnSpeed;
            RefreshSpeed();
            Play(RunState);
        }

        private void RefreshSpeed()
        {
            agent.speed = walkSpeed * (boosted ? settings.boostedMonsterSpeedFactor : settings.monsterSpeedFactor);
        }

        public void SetBoosted(bool value)
        {
            if (dead || boosted == value) return;
            boosted = value;
            if (agent != null && agent.enabled) RefreshSpeed();
            ApplyTint(value ? boostTint : Color.white, value);
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (dead) return;
            hp -= amount;
            if (hp <= 0f)
            {
                Die();
                return;
            }
            attack.Cancel();
            hurtUntil = Time.time + hurtSeconds;
            flashUntil = Time.time + damageFlashSeconds;
            ApplyTint(Color.red, true);
            Play(HurtState, true);
            if (hurtSound != null) AudioSource.PlayClipAtPoint(hurtSound, transform.position);
        }

        // The Witch died: stop where it stands.
        public void DropLifeless()
        {
            if (!dead) Die();
        }

        private void Die()
        {
            dead = true;
            hp = 0f;
            boosted = false;
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            ApplyTint(Color.white, false);
            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null) rigidbody.isKinematic = true;
            Ragdoll();
            if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);
            if (director != null) director.MonsterDied(this);
        }

        // Animator off, alive colliders off, bones become a physics ragdoll that ignores the player and the live monsters.
        private void Ragdoll()
        {
            if (EnemyDeathRules.OutcomeFor(EnemyKind.Monster) != DeathOutcome.Ragdoll) return;
            var ignore = new System.Collections.Generic.List<Collider>();
            var movement = player != null ? player.GetComponentsInChildren<Collider>() : new Collider[0];
            ignore.AddRange(movement);
            foreach (var other in FindObjectsByType<MonsterCustomer>(FindObjectsSortMode.None))
            {
                if (other != this) ignore.AddRange(other.GetComponentsInChildren<Collider>());
            }
            var ragdoll = gameObject.AddComponent<MonsterRagdoll>();
            ragdoll.Activate(animator, GetComponentsInChildren<Collider>(), ignore.ToArray());
        }

        private void Play(string state, bool restart = false)
        {
            if (animator == null) return;
            if (!restart && animator.GetCurrentAnimatorStateInfo(0).IsName(state)) return;
            animator.Play(state, 0, 0f);
        }

        private void ApplyTint(Color color, bool emissive)
        {
            tinted = emissive;
            RendererTint.Apply(renderers, block, color, emissive ? color * 1.5f : Color.black);
        }

        // Two small emissive spheres and a tiny point light parented to the head bone.
        private void BuildEyes()
        {
            var head = FindHead();
            if (head == null) head = transform;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = shader != null ? new Material(shader) : null;
            if (material != null) material.SetColor("_BaseColor", eyeColor);
            // The head bone inherits the rig's scale; counter it so sizes are in world metres.
            float scale = Mathf.Max(head.lossyScale.x, 1e-4f);
            for (int i = 0; i < 2; i++)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = i == 0 ? "EyeLeft" : "EyeRight";
                Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(head, false);
                eye.transform.localScale = Vector3.one * eyeSize / scale;
                eye.transform.localPosition = new Vector3((i == 0 ? -1f : 1f) * eyeSpacing, 0.08f, 0.1f) / scale;
                if (material != null) eye.GetComponent<Renderer>().sharedMaterial = material;
            }
            var lightObject = new GameObject("EyeGlow");
            lightObject.transform.SetParent(head, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.08f, 0.15f) / scale;
            var glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = eyeColor;
            glow.range = 2f;
            glow.intensity = 1.5f;
            glow.shadows = LightShadows.None;
        }

        private Transform FindHead()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("Head")) return t;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (!dead && director != null) director.MonsterDied(this);
        }
    }
}
