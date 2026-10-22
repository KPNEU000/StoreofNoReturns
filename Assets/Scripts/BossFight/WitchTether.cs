using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // Phase1 only: every interval the Witch links a purple beam to one random living monster, boosting it for a few seconds.
    [RequireComponent(typeof(LineRenderer))]
    public class WitchTether : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private Transform witch;
        [SerializeField]
        private Vector3 witchChestOffset = new Vector3(0f, 1.3f, 0f);
        [SerializeField]
        private Vector3 monsterChestOffset = new Vector3(0f, 1.2f, 0f);

        private TetherSchedule schedule;
        private LineRenderer line;
        private MonsterCustomer target;

        private void Awake()
        {
            var settings = config != null ? config.ToSettings() : new BossFightSettings();
            schedule = new TetherSchedule(settings);
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.enabled = false;
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (witch == null)
            {
                var boss = FindFirstObjectByType<WitchBoss>();
                if (boss != null) witch = boss.transform;
            }
        }

        private void Update()
        {
            if (director == null || director.Phase != BossPhase.Phase1)
            {
                EndTether();
                schedule.Reset();
                return;
            }
            int change = schedule.Tick(Time.deltaTime);
            if (change > 0) StartTether();
            else if (change < 0) EndTether();

            if (target != null && !target.IsAlive) EndTether();
            if (target != null && witch != null)
            {
                line.SetPosition(0, witch.position + witchChestOffset);
                line.SetPosition(1, target.transform.position + monsterChestOffset);
            }
        }

        private void StartTether()
        {
            var candidates = new List<MonsterCustomer>();
            foreach (var monster in director.Monsters)
            {
                if (monster is MonsterCustomer customer && customer.IsAlive) candidates.Add(customer);
            }
            if (candidates.Count == 0) return;
            target = candidates[Random.Range(0, candidates.Count)];
            target.SetBoosted(true);
            line.enabled = true;
        }

        private void EndTether()
        {
            if (target != null) target.SetBoosted(false);
            target = null;
            line.enabled = false;
        }
    }
}
