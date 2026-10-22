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
            if (prefab == null) return 0;
            int spawned = 0;
            for (int i = 0; i < count && alive.Count < cap; i++)
            {
                var offset = Quaternion.Euler(0f, 360f * i / Mathf.Max(count, 1), 0f) * Vector3.forward * (i == 0 ? 0f : 1f);
                var enemy = Instantiate(prefab, position + offset, Quaternion.identity);
                enemy.Init(this, spawnIndex++);
                alive.Add(enemy);
                spawned++;
            }
            return spawned;
        }

        public void Unregister(PossessedClothing enemy)
        {
            alive.Remove(enemy);
        }
    }
}
