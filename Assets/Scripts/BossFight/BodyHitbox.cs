using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // On every collider of a dragged body: damage to the body is damage to its monster, and the body jolts.
    public class BodyHitbox : MonoBehaviour, IDamageable
    {
        private IDamageable monster;
        private Rigidbody body;
        private float joltImpulse;

        public bool IsAlive => monster != null && monster.IsAlive;

        public void Bind(IDamageable owner, float impulse)
        {
            monster = owner;
            joltImpulse = impulse;
            if (body == null) body = GetComponent<Rigidbody>();
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (!DraggedBodyRules.Forward(monster, amount, from)) return;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.AddForce(DraggedBodyRules.JoltDirection(from, body.worldCenterOfMass) * joltImpulse, ForceMode.Impulse);
            }
        }
    }
}
