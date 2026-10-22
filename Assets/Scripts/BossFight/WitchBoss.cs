using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // The Witch: floats by the door, shielded until Phase2. Her HP lives in the director's phase machine.
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
        private float bobAmplitude = 0.08f;
        [SerializeField]
        private float bobSpeed = 1.5f;
        [SerializeField]
        private float damageFlashSeconds = 0.15f;

        private Vector3 floatPosition;
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
            transform.position += Vector3.up * settings.witchHoverHeight;
            floatPosition = transform.position;
            var all = GetComponentsInChildren<Renderer>(true);
            renderers = System.Array.FindAll(all, r => shield == null || !r.transform.IsChildOf(shield.transform));
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (director != null) director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (director != null) director.PhaseChanged -= OnPhaseChanged;
        }

        private void Start()
        {
            if (shield != null) shield.SetActive(true);
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
