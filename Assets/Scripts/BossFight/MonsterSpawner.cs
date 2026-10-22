using UnityEngine;

namespace BossFight
{
    // Answers BossDirector.MonsterSpawnRequested: one monster per angered customer, waiting outside the door
    // and set loose one after another.
    public class MonsterSpawner : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private MonsterCustomer prefab;
        [SerializeField]
        private Transform spawnPoint;

        private BossFightSettings settings;

        public Transform SpawnPoint => spawnPoint;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
        }

        private void OnEnable()
        {
            if (director != null) director.MonsterSpawnRequested += Spawn;
        }

        private void OnDisable()
        {
            if (director != null) director.MonsterSpawnRequested -= Spawn;
        }

        private void Spawn(int count)
        {
            if (prefab == null || spawnPoint == null) return;
            var reputationManager = FindFirstObjectByType<ReputationManager>();
            float reputation = reputationManager != null ? reputationManager.CurrentReputation : 50f;
            count = Mathf.Min(count, settings.maxMonsters);
            var names = new System.Collections.Generic.List<string>(AngeredCustomerRegistry.Names);
            for (int i = 0; i < count; i++)
            {
                var side = (i % 2 == 0 ? 1f : -1f) * Mathf.Ceil(i / 2f) * 0.8f;
                var position = spawnPoint.position + spawnPoint.right * side;
                var monster = Instantiate(prefab, position, spawnPoint.rotation);
                monster.Init(director, i, i * settings.monsterSpawnStaggerSeconds, reputation, i < names.Count ? names[i] : null);
            }
        }
    }
}
