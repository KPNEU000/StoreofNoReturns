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
        // Kept so existing scene data still deserializes; the pose now comes from SwordPose.
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
            if (swooshSound != null) Sfx.Play(swooshSound, transform.position, 0.2625f / Sfx.Master, 1f, 0.12f, 0f); // 2026-10-23: 25% under 0.35
        }

        private void EndSlash()
        {
            slashing = false;
            IsSlashing = false;
            if (movement != null) movement.SpeedMultiplier = 1f;
        }

        // Round 13: a flat horizontal fan from the player's body along the way they face, measured to the nearest edge
        // of each enemy whatever its height. It used to measure to collider centres (a garment's hanger), from a
        // sword that dips with the camera, so aiming low or at a garment's chest fell short.
        private const float SlashColumnHeight = 6f;

        private void DealDamage()
        {
            var player = movement != null ? movement.transform : transform;
            Vector3 origin = player.position;
            Vector3 forward = player.forward;
            var bottom = origin + Vector3.down * SlashColumnHeight * 0.5f;
            var top = origin + Vector3.up * SlashColumnHeight * 0.5f;
            var colliders = Physics.OverlapCapsule(bottom, top, settings.slashReach, ~0, QueryTriggerInteraction.Ignore);
            var found = new List<IDamageable>();
            var positions = new List<Vector3>();
            foreach (var col in colliders)
            {
                var damageable = col.GetComponentInParent<IDamageable>();
                // A dragged body's bones all forward to its monster: one slash hit the monster up to three times.
                if (damageable is BodyHitbox body && body.Owner != null) damageable = body.Owner;
                if (damageable == null || !damageable.IsAlive) continue;
                int index = found.IndexOf(damageable);
                var position = PlanarClosestPoint(col, origin);
                if (index < 0)
                {
                    found.Add(damageable);
                    positions.Add(position);
                }
                else if ((position - origin).sqrMagnitude < (positions[index] - origin).sqrMagnitude)
                {
                    positions[index] = position; // aim at whichever part of it is closest
                }
            }
            foreach (int i in rules.SelectTargets(origin, forward, positions))
            {
                found[i].TakeDamage(settings.slashDamage, origin);
                if (hitSound != null) Sfx.Play(hitSound, positions[i]);
            }
        }

        // The point of `col` horizontally closest to `origin`, taken at the collider's own height.
        private static Vector3 PlanarClosestPoint(Collider col, Vector3 origin)
        {
            var bounds = col.bounds;
            var level = new Vector3(origin.x, bounds.center.y, origin.z);
            bool convex = !(col is MeshCollider mesh) || mesh.convex;
            return convex ? col.ClosestPoint(level) : bounds.ClosestPoint(level);
        }

        // Upright -> point forward (windup) -> right-to-left sweep around the hit -> upright; pose comes from SwordPose.
        private void Animate()
        {
            if (pivot == null) return;
            ApplyPose(pivot, restPosition, restEuler, elapsed / Mathf.Max(rules.Duration, 1e-4f), rules.HitTime / Mathf.Max(rules.Duration, 1e-4f));
        }

        public static void ApplyPose(Transform pivot, Vector3 restPosition, Vector3 restEuler, float t, float hitFraction)
        {
            var pose = SwordPose.Evaluate(t, hitFraction);
            pivot.localRotation = Quaternion.Euler(restEuler) * pose.rotation;
            pivot.localPosition = restPosition + pose.positionOffset;
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
