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
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
            }
            // No baked NavMesh asset is committed; the shop is built into a NavMesh at runtime.
            navMeshSurface.BuildNavMesh();
        }
    }
}
