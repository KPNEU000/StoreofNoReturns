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
            if (player != null)
            {
                NavAgents.ExcludeFromNavMesh(player.gameObject);
                var guard = player.GetComponent<PlayerBoundsGuard>();
                if (guard == null) guard = player.gameObject.AddComponent<PlayerBoundsGuard>();
                guard.Init(CeilingBounds());
            }
            var witch = FindFirstObjectByType<WitchBoss>();
            if (witch != null) NavAgents.ExcludeFromNavMesh(witch.gameObject);
            MarkOutside();
            // No baked NavMesh asset is committed; the shop is built into a NavMesh at runtime.
            navMeshSurface.BuildNavMesh();
        }

        // Paints everything beyond the front door with NavAgents.OutsideArea: a big box whose near face sits just
        // outside the doorway, facing away from the store's centre. Enemies use it to stay in the store.
        private void MarkOutside()
        {
            var door = GameObject.Find("DoorBarrier");
            var ceiling = CeilingBounds();
            if (door == null || !ceiling.HasValue) return;
            var outward = door.transform.position - ceiling.Value.center;
            outward.y = 0f;
            if (outward.sqrMagnitude < 1e-4f) return;
            outward.Normalize();
            const float depth = 120f;
            var go = new GameObject("OutsideStoreArea");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(door.transform.position + outward * (OutsideGap + depth * 0.5f), Quaternion.LookRotation(outward));
            var volume = go.AddComponent<NavMeshModifierVolume>();
            volume.size = new Vector3(200f, 60f, depth);
            volume.center = Vector3.zero;
            volume.area = NavAgents.OutsideArea;
        }

        // How far past the doorway the outside zone starts.
        private const float OutsideGap = 0.6f;

        // The store's ceiling (before any cave-in) marks the store's footprint for the player guard.
        private static Bounds? CeilingBounds()
        {
            var ceiling = GameObject.Find("Ceiling");
            var renderer = ceiling != null ? ceiling.GetComponent<Renderer>() : null;
            return renderer != null ? renderer.bounds : (Bounds?)null;
        }
    }
}
