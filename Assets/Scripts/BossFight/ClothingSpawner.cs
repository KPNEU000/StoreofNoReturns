using System.Collections.Generic;
using UnityEngine;

namespace BossFight
{
    // Spawns possessed clothing up to the alive cap. Boss Scene only; the boss director drives it later.
    public class ClothingSpawner : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private PossessedClothing prefab;
        [SerializeField]
        private Transform spawnPoint;
        [SerializeField]
        private bool autoSpawnForTesting = true;
        [SerializeField]
        private int autoSpawnCount = 3;

        private readonly List<PossessedClothing> alive = new List<PossessedClothing>();
        private int cap = 15;
        private int spawnIndex;

        public int AliveCount => alive.Count;
        public int Cap => cap;

        private void Awake()
        {
            if (config != null)
            {
                cap = config.ToSettings().maxClothingAlive;
            }
        }

        private void Start()
        {
            if (autoSpawnForTesting && spawnPoint != null)
            {
                Spawn(spawnPoint.position, autoSpawnCount);
            }
        }

        // Returns how many were actually spawned (fewer than count when the cap is reached).
        public int Spawn(Vector3 position, int count)
        {
            return Spawn(position, count, null);
        }

        // `garments` are the releasing rack's baked garments, worn in turn; null/empty keeps the prefab's fallback.
        public int Spawn(Vector3 position, int count, GameObject[] garments)
        {
            if (prefab == null) return 0;
            int spawned = 0;
            for (int i = 0; i < count && alive.Count < cap; i++)
            {
                var offset = Quaternion.Euler(0f, 360f * i / Mathf.Max(count, 1), 0f) * Vector3.forward * (i == 0 ? 0f : 1f);
                var enemy = Instantiate(prefab, position + offset, Quaternion.identity);
                enemy.Init(this, spawnIndex++);
                if (garments != null && garments.Length > 0) enemy.Dress(garments[i % garments.Length]);
                alive.Add(enemy);
                spawned++;
            }
            return spawned;
        }

        // A wave all around `center` (a possessed rack), each costume placed on the NavMesh at a random spot on the
        // ring, with `hpMultiplier` applied. Returns the costumes spawned (fewer than count at the alive cap).
        public System.Collections.Generic.List<PossessedClothing> SpawnRing(Vector3 center, int count, GameObject[] garments,
            float minRadius, float maxRadius, float hpMultiplier)
        {
            var spawned = new System.Collections.Generic.List<PossessedClothing>();
            if (prefab == null) return spawned;
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < count && alive.Count < cap; i++)
            {
                float angle = start + 360f * i / Mathf.Max(count, 1) + Random.Range(-12f, 12f);
                var point = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(minRadius, maxRadius);
                if (UnityEngine.AI.NavMesh.SamplePosition(point, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) point = hit.position;
                var enemy = Instantiate(prefab, point, Quaternion.identity);
                enemy.Init(this, spawnIndex++);
                enemy.ScaleHealth(hpMultiplier);
                if (garments != null && garments.Length > 0) enemy.Dress(garments[i % garments.Length]);
                alive.Add(enemy);
                spawned.Add(enemy);
            }
            return spawned;
        }

        // One costume that cannot hurt the player and patrols between two points; null when the cap is reached.
        public PossessedClothing SpawnHarmless(Vector3 position, Vector3 patrolFrom, Vector3 patrolTo)
        {
            if (prefab == null || alive.Count >= cap) return null;
            var enemy = Instantiate(prefab, position, Quaternion.identity);
            enemy.Init(this, spawnIndex++);
            enemy.MakeHarmless(patrolFrom, patrolTo);
            enemy.Dress(PossessableRack.NearestGarment(position));
            alive.Add(enemy);
            return enemy;
        }

        public void Unregister(PossessedClothing enemy)
        {
            alive.Remove(enemy);
        }
    }
}
