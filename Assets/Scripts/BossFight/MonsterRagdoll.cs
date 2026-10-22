using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // A monster's corpse: rigid bodies, capsules and joints built from its mixamorig bones (the Ragdoll Wizard layout).
    // Nothing exists while the monster is alive; Activate switches the Animator off and drops the body as a physics ragdoll.
    public class MonsterRagdoll : MonoBehaviour
    {
        private readonly List<Rigidbody> bodies = new List<Rigidbody>();
        private float startedAt;
        private bool active;
        private bool settled;

        public bool Active => active;
        public bool Settled => settled;
        public int BodyCount => bodies.Count;

        public void Activate(Animator animator, IEnumerable<Collider> aliveColliders, Collider[] ignoreAlways)
        {
            if (active) return;
            active = true;
            if (animator != null) animator.enabled = false;
            foreach (var collider in aliveColliders)
            {
                if (collider != null) collider.enabled = false;
            }
            Build();
            // The corpse never blocks the player or the live monsters.
            var corpseColliders = new List<Collider>();
            foreach (var body in bodies) corpseColliders.AddRange(body.GetComponents<Collider>());
            foreach (var other in ignoreAlways)
            {
                if (other == null) continue;
                foreach (var mine in corpseColliders) Physics.IgnoreCollision(mine, other, true);
            }
            startedAt = Time.time;
        }

        private void Build()
        {
            var byName = new Dictionary<string, Transform>();
            foreach (var spec in EnemyDeathRules.Bones)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (EnemyDeathRules.IsBone(t.name, spec.Name)) { byName[spec.Name] = t; break; }
                }
            }
            var made = new Dictionary<string, Rigidbody>();
            foreach (var spec in EnemyDeathRules.Bones)
            {
                if (!byName.TryGetValue(spec.Name, out var bone)) continue;
                var body = bone.gameObject.AddComponent<Rigidbody>();
                body.mass = spec.Mass;
                body.linearDamping = 0.2f;
                body.angularDamping = 0.5f;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.None;
                AddCapsule(bone, byName, spec.Name);
                made[spec.Name] = body;
                bodies.Add(body);
            }
            foreach (var spec in EnemyDeathRules.Bones)
            {
                if (spec.Parent == "" || !made.TryGetValue(spec.Name, out var body) || !made.TryGetValue(spec.Parent, out var parent)) continue;
                var joint = body.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parent;
                joint.enableProjection = true;
                joint.enablePreprocessing = false;
                joint.lowTwistLimit = new SoftJointLimit { limit = -30f };
                joint.highTwistLimit = new SoftJointLimit { limit = 30f };
                joint.swing1Limit = new SoftJointLimit { limit = 40f };
                joint.swing2Limit = new SoftJointLimit { limit = 40f };
            }
        }

        // A capsule from the bone to its first child bone (or a short stub for the head/end bones), in the bone's local space.
        private static void AddCapsule(Transform bone, Dictionary<string, Transform> byName, string name)
        {
            Transform end = null;
            foreach (var spec in EnemyDeathRules.Bones)
            {
                if (spec.Parent == name && byName.TryGetValue(spec.Name, out var child)) { end = child; break; }
            }
            Vector3 localEnd;
            if (end != null && name != "Hips")
            {
                localEnd = bone.InverseTransformPoint(end.position);
            }
            else
            {
                float s = Mathf.Max(bone.lossyScale.x, 1e-4f);
                localEnd = bone.InverseTransformDirection(name == "Head" ? Vector3.up : name == "Hips" ? Vector3.up : Vector3.down) * (name == "Head" ? 0.25f : 0.3f) / s;
            }
            float length = localEnd.magnitude;
            if (length < 1e-4f) length = 0.1f;
            float scale = Mathf.Max(bone.lossyScale.x, 1e-4f);
            var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            var axis = Mathf.Abs(localEnd.x) >= Mathf.Abs(localEnd.y) && Mathf.Abs(localEnd.x) >= Mathf.Abs(localEnd.z) ? 0
                : Mathf.Abs(localEnd.y) >= Mathf.Abs(localEnd.z) ? 1 : 2;
            capsule.direction = axis;
            capsule.center = localEnd * 0.5f;
            capsule.height = length;
            capsule.radius = Mathf.Min(0.12f / scale, length * 0.45f);
        }

        private void FixedUpdate()
        {
            if (!active || settled) return;
            float fastest = 0f;
            foreach (var body in bodies)
            {
                if (body != null) fastest = Mathf.Max(fastest, body.linearVelocity.magnitude);
            }
            if (EnemyDeathRules.HasSettled(Time.time - startedAt, fastest)) Freeze();
        }

        // Settled corpses stop costing physics and can never block anything afterwards.
        private void Freeze()
        {
            settled = true;
            foreach (var body in bodies)
            {
                if (body == null) continue;
                foreach (var joint in body.GetComponents<Joint>()) Destroy(joint);
                foreach (var collider in body.GetComponents<Collider>()) Destroy(collider);
                Destroy(body);
            }
            bodies.Clear();
        }
    }
}
