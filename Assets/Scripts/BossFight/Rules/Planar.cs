using UnityEngine;

namespace BossFight.Rules
{
    // Floor-plane direction helper shared by the placement rules.
    public static class Planar
    {
        // `v` flattened onto the floor (y = 0) and normalized; world forward when it has no horizontal length.
        public static Vector3 Direction(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
