using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // On MonsterCustomer.prefab: spawns the ragdolled body of the angered customer and drags it by one wrist.
    public class MonsterDrag : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private GameObject bodyPrefab;
        [SerializeField]
        private GameObject[] costumePrefabs;
        // Approved fallback: no physics, the body hangs from the hand in a fixed pose and sways.
        [SerializeField]
        private bool useFixedPoseFallback;
        [SerializeField]
        private Vector3 fixedPoseLocalEuler = new Vector3(0f, 0f, 90f);
        [SerializeField]
        private Vector3 fixedPoseLocalOffset = new Vector3(0f, -0.9f, 0f);

        private MonsterCustomer monster;
        private BossFightSettings settings;
        private Transform hand;
        private GameObject body;
        private Rigidbody wrist;
        private Joint joint;
        private bool released;

        public GameObject Body => body;

        public void Begin(MonsterCustomer owner, string customerName)
        {
            monster = owner;
            settings = config != null ? config.ToSettings() : new BossFightSettings();
            hand = FindHand();
            if (bodyPrefab == null || hand == null) return;

            body = Instantiate(bodyPrefab, hand.position, hand.rotation);
            body.name = "DraggedCustomer";
            var costume = ResolveCostume(customerName);
            if (costume != null) DressBody(costume);

            int layer = DraggedBodyRules.ResolveLayer();
            foreach (var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var hitbox in body.GetComponentsInChildren<BodyHitbox>(true)) hitbox.Bind(monster, settings.bodyJoltImpulse);

            if (useFixedPoseFallback) AttachFixedPose();
            else AttachPhysics(layer);
        }

        private void Update()
        {
            if (body == null || released) return;
            if (!monster.IsAlive)
            {
                Release();
                return;
            }
            if (useFixedPoseFallback)
            {
                float sway = DraggedBodyRules.SwayAngle(Time.time, settings.fixedPoseSwayDegrees, settings.fixedPoseSwayHertz);
                body.transform.localRotation = Quaternion.Euler(fixedPoseLocalEuler) * Quaternion.Euler(sway, 0f, 0f);
            }
        }

        // The costume rides on the hips so it follows the body as it flops.
        private void DressBody(GameObject costume)
        {
            Transform parent = body.transform;
            foreach (var rb in body.GetComponentsInChildren<Rigidbody>(true))
            {
                var lower = rb.name.ToLowerInvariant();
                if (lower.Contains("hips") || lower.Contains("pelvis"))
                {
                    parent = rb.transform;
                    break;
                }
            }
            var worn = Instantiate(costume, parent);
            worn.transform.localPosition = Vector3.zero;
            worn.transform.localRotation = Quaternion.identity;
            foreach (var c in worn.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }

        private void AttachPhysics(int layer)
        {
            DraggedBodyRules.ConfigureCollisionMatrix(layer);
            var bodies = body.GetComponentsInChildren<Rigidbody>(true);
            wrist = FindWrist(bodies);
            if (wrist == null) return;

            // Kinematic anchor on the hand bone; the body hangs from it by the wrist.
            var anchor = new GameObject("DragAnchor");
            anchor.transform.SetParent(hand, false);
            anchor.transform.position = wrist.position;
            var anchorBody = anchor.AddComponent<Rigidbody>();
            anchorBody.isKinematic = true;
            var characterJoint = wrist.gameObject.AddComponent<CharacterJoint>();
            characterJoint.connectedBody = anchorBody;
            characterJoint.autoConfigureConnectedAnchor = false;
            characterJoint.anchor = Vector3.zero;
            characterJoint.connectedAnchor = Vector3.zero;
            characterJoint.enableProjection = true;
            joint = characterJoint;

            // Layers cannot express "not the player or the monster" when those sit on Default with the ground.
            var ignoreThese = new List<Collider>();
            foreach (var controller in FindObjectsByType<CharacterController>(FindObjectsSortMode.None)) ignoreThese.Add(controller);
            ignoreThese.AddRange(GetComponentsInChildren<Collider>(true));
            foreach (var own in body.GetComponentsInChildren<Collider>(true))
            {
                foreach (var other in ignoreThese)
                {
                    if (other != null) Physics.IgnoreCollision(own, other, true);
                }
            }
        }

        private void AttachFixedPose()
        {
            foreach (var rb in body.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
            body.transform.SetParent(hand, false);
            body.transform.localPosition = fixedPoseLocalOffset;
            body.transform.localRotation = Quaternion.Euler(fixedPoseLocalEuler);
        }

        // The monster died: break the joint and let the body lie limp where it is.
        private void Release()
        {
            released = true;
            if (joint != null) Destroy(joint);
            if (useFixedPoseFallback)
            {
                body.transform.SetParent(null, true);
                foreach (var rb in body.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = false;
            }
        }

        private GameObject ResolveCostume(string customerName)
        {
            if (costumePrefabs == null || costumePrefabs.Length == 0) return null;
            if (!string.IsNullOrEmpty(customerName))
            {
                foreach (var prefab in costumePrefabs)
                {
                    if (prefab != null && NamesMatch(prefab.name, customerName)) return prefab;
                }
            }
            return costumePrefabs[Random.Range(0, costumePrefabs.Length)];
        }

        public static bool NamesMatch(string costumeName, string customerName)
        {
            var costume = costumeName.ToLowerInvariant();
            var customer = customerName.ToLowerInvariant().Replace("(clone)", "").Trim();
            return customer.Length > 0 && (costume.Contains(customer) || customer.Contains(costume));
        }

        private Transform FindHand()
        {
            Transform fallback = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                var lower = t.name.ToLowerInvariant();
                if (!lower.EndsWith("hand")) continue;
                if (lower.Contains("right")) return t;
                if (fallback == null) fallback = t;
            }
            return fallback;
        }

        private static Rigidbody FindWrist(Rigidbody[] bodies)
        {
            Rigidbody fallback = null;
            foreach (var rb in bodies)
            {
                var lower = rb.name.ToLowerInvariant();
                if (!lower.EndsWith("hand")) continue;
                if (lower.Contains("right") || lower.EndsWith("_r") || lower.EndsWith(".r")) return rb;
                if (fallback == null) fallback = rb;
            }
            return fallback != null ? fallback : (bodies.Length > 0 ? bodies[bodies.Length - 1] : null);
        }
    }
}
