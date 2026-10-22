using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight
{
    // Left click slash. The blade is a placeholder cube floating in view as a child of the camera.
    public class PlayerSword : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private PlayerMovement movement;
        [SerializeField]
        private Transform pivot;
        [SerializeField]
        private AudioClip swooshSound;
        [SerializeField]
        private AudioClip hitSound;
        [SerializeField]
        private Vector3 restEuler = new Vector3(0f, 0f, 0f);
        [SerializeField]
        private Vector3 windupEuler = new Vector3(0f, 0f, -80f);
        [SerializeField]
        private Vector3 sweepEuler = new Vector3(0f, 0f, 80f);

        public static bool IsSlashing { get; private set; }

        private SlashRules rules;
        private BossFightSettings settings;
        private float elapsed;
        private bool slashing;
        private bool hitDone;
        private Vector3 restPosition;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            rules = new SlashRules(settings);
            if (pivot != null) restPosition = pivot.localPosition;
        }

        private void OnDisable()
        {
            EndSlash();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (!slashing)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                {
                    StartSlash();
                }
                else
                {
                    Idle();
                    return;
                }
            }

            elapsed += Time.deltaTime;
            if (!hitDone && elapsed >= rules.HitTime)
            {
                hitDone = true;
                DealDamage();
            }
            if (rules.PhaseAt(elapsed) == SlashPhase.Done)
            {
                EndSlash();
                Idle();
                return;
            }
            Animate();
        }

        private void StartSlash()
        {
            slashing = true;
            hitDone = false;
            elapsed = 0f;
            IsSlashing = true;
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (movement != null) movement.SpeedMultiplier = rules.MoveSpeed(movement.speed) / movement.speed;
            if (swooshSound != null) AudioSource.PlayClipAtPoint(swooshSound, transform.position);
        }

        private void EndSlash()
        {
            slashing = false;
            IsSlashing = false;
            if (movement != null) movement.SpeedMultiplier = 1f;
        }

        private void DealDamage()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            var colliders = Physics.OverlapSphere(origin, settings.slashReach, ~0, QueryTriggerInteraction.Ignore);
            var found = new List<IDamageable>();
            var positions = new List<Vector3>();
            foreach (var col in colliders)
            {
                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || found.Contains(damageable)) continue;
                found.Add(damageable);
                positions.Add(col.transform.position);
            }
            foreach (int i in rules.SelectTargets(origin, forward, positions))
            {
                found[i].TakeDamage(settings.slashDamage, origin);
                if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, positions[i]);
            }
        }

        // Upright rest -> raised during windup -> fast horizontal right-to-left sweep -> eases back.
        private void Animate()
        {
            if (pivot == null) return;
            Quaternion rest = Quaternion.Euler(restEuler);
            if (elapsed < rules.HitTime)
            {
                float t = elapsed / Mathf.Max(rules.HitTime, 1e-4f);
                pivot.localRotation = Quaternion.Slerp(rest, Quaternion.Euler(windupEuler), t);
            }
            else
            {
                float t = (elapsed - rules.HitTime) / Mathf.Max(rules.Duration - rules.HitTime, 1e-4f);
                float sweep = Mathf.Clamp01(t * 4f);
                float back = Mathf.Clamp01((t - 0.25f) / 0.75f);
                var swept = Quaternion.Slerp(Quaternion.Euler(windupEuler), Quaternion.Euler(sweepEuler), sweep);
                pivot.localRotation = Quaternion.Slerp(swept, rest, back);
            }
            pivot.localPosition = restPosition;
        }

        // Procedural bob/sway while not slashing.
        private void Idle()
        {
            if (pivot == null) return;
            float bob = Mathf.Sin(Time.time * 1.6f) * 0.01f;
            pivot.localPosition = restPosition + new Vector3(0f, bob, 0f);
            pivot.localRotation = Quaternion.Euler(restEuler + new Vector3(0f, 0f, Mathf.Sin(Time.time * 1.1f) * 1.5f));
        }
    }
}
