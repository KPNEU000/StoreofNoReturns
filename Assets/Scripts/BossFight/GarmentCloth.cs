using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // Turns a baked garment (bone-less SkinnedMeshRenderer, upright, unscaled) into Unity Cloth hanging from its parent.
    public static class GarmentCloth
    {
        public const string PlayerColliderName = "ClothCollider";

        public static Cloth Attach(SkinnedMeshRenderer skin, BossFightSettings settings, CapsuleCollider player)
        {
            var cloth = skin.GetComponent<Cloth>();
            if (cloth == null) cloth = skin.gameObject.AddComponent<Cloth>();
            cloth.clothSolverFrequency = settings.clothSolverFrequency;
            cloth.stretchingStiffness = settings.clothStretchingStiffness;
            cloth.bendingStiffness = settings.clothBendingStiffness;
            cloth.damping = settings.clothDamping;
            cloth.worldVelocityScale = settings.clothWorldVelocityScale;
            cloth.worldAccelerationScale = settings.clothWorldAccelerationScale;
            cloth.randomAcceleration = Vector3.one * settings.clothFlapAcceleration;
            cloth.useGravity = true;
            cloth.useTethers = false;
            cloth.useVirtualParticles = 0f;
            cloth.selfCollisionDistance = 0f;
            cloth.enableContinuousCollision = false;
            cloth.sphereColliders = new ClothSphereColliderPair[0];
            cloth.capsuleColliders = player != null ? new[] { player } : new CapsuleCollider[0];

            var coefficients = cloth.coefficients;
            var vertices = cloth.vertices;
            if (vertices == null || vertices.Length != coefficients.Length) vertices = skin.sharedMesh != null ? skin.sharedMesh.vertices : new Vector3[0];
            if (vertices.Length == coefficients.Length && coefficients.Length > 0)
            {
                var distances = ClothPinRules.MaxDistances(vertices, settings.clothPinBand, settings.clothFreeDistance);
                for (int i = 0; i < coefficients.Length; i++) coefficients[i].maxDistance = distances[i];
                cloth.coefficients = coefficients;
            }
            else
            {
                Debug.LogWarning($"GarmentCloth: '{skin.name}' has {coefficients.Length} cloth coefficients for {vertices.Length} vertices; pins skipped");
            }
            return cloth;
        }

        // Trigger capsule matching the player's CharacterController, so garments drape over the player without
        // pushing anything (the sword's OverlapSphere ignores triggers).
        public static CapsuleCollider PlayerCapsule(Transform player)
        {
            if (player == null) return null;
            var existing = player.Find(PlayerColliderName);
            if (existing != null && existing.GetComponent<CapsuleCollider>() != null) return existing.GetComponent<CapsuleCollider>();
            var go = new GameObject(PlayerColliderName);
            go.transform.SetParent(player, false);
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                capsule.center = controller.center;
                capsule.radius = controller.radius;
                capsule.height = controller.height;
            }
            else
            {
                capsule.center = Vector3.up * 0.9f;
                capsule.radius = 0.4f;
                capsule.height = 1.8f;
            }
            return capsule;
        }
    }
}
