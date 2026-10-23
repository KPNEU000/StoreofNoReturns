using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // The Witch: stands on the floor beside the entrance, idling, shielded until Phase2. Her HP lives in the director's phase machine.
    public class WitchBoss : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private GameObject shield;
        [SerializeField]
        private ParticleSystem shatterBurst;
        [SerializeField]
        private AudioClip shatterSound;
        [SerializeField]
        private AudioClip hurtSound;
        [SerializeField]
        private Texture shieldTexture;
        [SerializeField]
        private float bobAmplitude = 0.08f;
        [SerializeField]
        private float bobSpeed = 1.5f;
        [SerializeField]
        private float damageFlashSeconds = 0.15f;

        private const string WalkingParameter = "Walking";

        private Vector3 floatPosition;
        private float hoverHeight;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float flashUntil;
        private bool flashing;
        private bool dead;

        public bool IsAlive => !dead;
        public bool Shielded => shield != null && shield.activeSelf;

        private void Awake()
        {
            var settings = BossFightConfig.SettingsOf(config);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            transform.localScale *= settings.witchHeightScale;
            hoverHeight = settings.witchHoverHeight;
            transform.position += Vector3.up * hoverHeight;
            floatPosition = transform.position;
            var all = GetComponentsInChildren<Renderer>(true);
            renderers = System.Array.FindAll(all, r => shield == null || !r.transform.IsChildOf(shield.transform));
            block = new MaterialPropertyBlock();
            Idle();
        }

        // WitchAnim's Walking bool defaults to true; she stands still, so force the idle state.
        private void Idle()
        {
            var animator = GetComponent<Animator>();
            if (animator == null) return;
            animator.applyRootMotion = false;
            animator.SetBool(WalkingParameter, false);
        }

        private void ApplyShieldTexture()
        {
            if (shield == null || shieldTexture == null) return;
            var shieldRenderer = shield.GetComponent<Renderer>();
            if (shieldRenderer == null) return;
            var shieldBlock = new MaterialPropertyBlock();
            shieldBlock.SetTexture("_BaseMap", shieldTexture);
            shieldBlock.SetTexture("_MainTex", shieldTexture);
            shieldRenderer.SetPropertyBlock(shieldBlock);
        }

        private void OnEnable()
        {
            if (director != null) director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (director != null) director.PhaseChanged -= OnPhaseChanged;
        }

        // Round 10: before the opening she is not in the store at all (her shield glowing by the door gave her away).
        private bool absent;

        public void SetPresent(bool present)
        {
            absent = !present;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = present;
            foreach (var c in GetComponents<Collider>()) c.enabled = present;
            if (!present && shield != null) shield.SetActive(false);
        }

        private void Start()
        {
            if (shield != null && !absent) shield.SetActive(true);
            ApplyShieldTexture();
            Idle();
        }

        // The tutorial drops the shield so the player can land one hit; it goes back up as she retreats.
        public void SetShield(bool up)
        {
            if (shield != null) shield.SetActive(up);
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (dead || director == null) return;
            if (!director.DamageWitch(amount)) return;
            flashUntil = Time.time + damageFlashSeconds;
            Tint(Color.red);
            if (hurtSound != null) AudioSource.PlayClipAtPoint(hurtSound, transform.position);
        }

        private void OnPhaseChanged(BossPhase phase)
        {
            if (phase == BossPhase.Phase2) Shatter();
            else if (phase == BossPhase.Won) Die();
        }

        private void Shatter()
        {
            if (shield == null || !shield.activeSelf) return;
            shield.SetActive(false);
            if (shatterBurst != null) shatterBurst.Play();
            if (shatterSound != null) AudioSource.PlayClipAtPoint(shatterSound, transform.position);
        }

        // Falls out of the air; the director handles the score and scene change.
        private void Die()
        {
            if (dead) return;
            dead = true;
            Tint(Color.red);
            var animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            if (GetComponent<Rigidbody>() == null) gameObject.AddComponent<Rigidbody>().mass = 5f;
        }

        private void Update()
        {
            if (flashing && !dead && Time.time >= flashUntil) ClearTint();
            if (dead) return;
            // Only bob when configured to hover; on the floor she stays put.
            if (hoverHeight <= 0f) return;
            transform.position = floatPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
        }

        private void ClearTint()
        {
            flashing = false;
            foreach (var r in renderers)
            {
                if (r != null) r.SetPropertyBlock(null);
            }
        }

        private void Tint(Color color)
        {
            flashing = true;
            RendererTint.Apply(renderers, block, color, null);
        }
    }
}
