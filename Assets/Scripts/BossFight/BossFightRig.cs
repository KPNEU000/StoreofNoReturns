using Unity.AI.Navigation;
using UnityEngine;

namespace BossFight
{
    // Root of BossFightRig.prefab. Holds only self-contained references;
    // scene references are assigned on the Boss Scene instance by the editor builder.
    [RequireComponent(typeof(NavMeshSurface))]
    public class BossFightRig : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private NavMeshSurface navMeshSurface;

        public BossFightConfig Config => config;
        public NavMeshSurface NavMeshSurface => navMeshSurface;

        private void Awake()
        {
            // Before any Start: CustomerSpawner.Start would otherwise send in the first shop customer.
            ShopShutdown.Apply(gameObject.scene);
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
            }
            // Boss Scene still carries MainScene's NavMeshSurfaces (two with bakes made with the door shut). Their
            // separate, unlinked meshes trapped monsters outside the doorway, so only the rig's mesh may exist.
            foreach (var surface in FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (surface != navMeshSurface) surface.enabled = false;
            }
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) NavAgents.ExcludeFromNavMesh(player.gameObject);
            var witch = FindFirstObjectByType<WitchBoss>();
            if (witch != null) NavAgents.ExcludeFromNavMesh(witch.gameObject);
            // No baked NavMesh asset is committed; the shop is built into a NavMesh at runtime.
            navMeshSurface.BuildNavMesh();
        }
    }
}
