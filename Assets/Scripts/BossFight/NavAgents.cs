using BossFight.Rules;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // Shared NavMesh hygiene for everything that moves in the fight.
    public static class NavAgents
    {
        // NavMesh area painted over everything beyond the front door (BossFightRig). Clothing never walks it;
        // monsters spawn out there and lose access the moment they first stand inside the store (playtest round 7:
        // they wandered back out of the door and around behind the player).
        public const int OutsideArea = 3;
        public const int OutsideMask = 1 << OutsideArea;
        public const int IndoorMask = NavMesh.AllAreas & ~OutsideMask;

        // Restricts the agent to the store once it is standing on indoor NavMesh. Returns true once restricted.
        public static bool KeepInsideOnceIn(NavMeshAgent agent)
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            if (agent.areaMask == IndoorMask) return true;
            if (!NavMesh.SamplePosition(agent.transform.position, out var hit, 0.6f, NavMesh.AllAreas)) return false;
            if ((hit.mask & OutsideMask) != 0) return false;
            agent.areaMask = IndoorMask;
            return true;
        }
        // Agents, the player, corpses, bodies and books must never be baked into the runtime NavMesh: each rebuild
        // would cut a hole under them, stranding agents until they re-path and snap out of it.
        public static void ExcludeFromNavMesh(GameObject root)
        {
            if (root == null) return;
            var modifier = root.GetComponent<NavMeshModifier>();
            if (modifier == null) modifier = root.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
        }

        // Snappier acceleration for the fast round-2 speeds, and varied avoidance priority so a crowd in a doorway
        // does not deadlock with every agent yielding to every other.
        public static void Configure(NavMeshAgent agent)
        {
            if (agent == null) return;
            agent.acceleration = Mathf.Max(agent.acceleration, 50f);
            agent.avoidancePriority = Random.Range(30, 70);
            agent.autoRepath = true;
        }

        // Requests a path toward `target` (snapped onto the NavMesh) only when the gate allows it.
        public static void Steer(NavMeshAgent agent, RepathGate gate, Vector3 target)
        {
            if (agent == null || !agent.isOnNavMesh || !gate.ShouldRepath(Time.time, target)) return;
            // Snap the goal onto ground this agent may walk (indoors only, for most of the fight).
            if (NavMesh.SamplePosition(target, out var hit, 2f, agent.areaMask)) target = hit.position;
            agent.SetDestination(target);
        }
    }
}
