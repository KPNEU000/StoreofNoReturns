using BossFight.Rules;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // Shared NavMesh hygiene for everything that moves in the fight.
    public static class NavAgents
    {
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
            if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) target = hit.position;
            agent.SetDestination(target);
        }
    }
}
