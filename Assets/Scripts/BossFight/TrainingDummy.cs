using UnityEngine;

namespace BossFight
{
    // Slash target for playtesting; flashes and respawns its health instead of dying.
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private float maxHealth = 50f;
        [SerializeField]
        private Renderer flashRenderer;

        private float health;
        private float flashUntil;
        private Color baseColor = Color.white;

        public bool IsAlive => health > 0f;

        private void Awake()
        {
            health = maxHealth;
            if (flashRenderer != null) baseColor = flashRenderer.material.color;
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            health -= amount;
            flashUntil = Time.time + 0.15f;
            if (health <= 0f) health = maxHealth;
        }

        private void Update()
        {
            if (flashRenderer == null) return;
            flashRenderer.material.color = Time.time < flashUntil ? Color.red : baseColor;
        }
    }
}
