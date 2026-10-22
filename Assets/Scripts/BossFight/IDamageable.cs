using UnityEngine;

namespace BossFight
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 from);
        bool IsAlive { get; }
    }
}
