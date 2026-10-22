using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // While the fight is in Phase1/Phase2, possesses a rack every wave interval and releases its costumes.
    public class ClothingWaveDriver : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private ClothingSpawner spawner;
        [SerializeField]
        private BossFightRig rig;

        private WaveScheduler scheduler;
        private readonly List<PossessableRack> racks = new List<PossessableRack>();
        private int reuseIndex;

        private void Awake()
        {
            var settings = BossFightConfig.SettingsOf(config);
            scheduler = new WaveScheduler(settings);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (spawner == null) spawner = GetComponent<ClothingSpawner>();
            if (rig == null) rig = FindFirstObjectByType<BossFightRig>();
            racks.AddRange(FindObjectsByType<PossessableRack>(FindObjectsSortMode.None));
        }

        private void Update()
        {
            if (director == null || spawner == null) return;
            var phase = director.Phase;
            if (phase == BossPhase.Won || phase == BossPhase.Lost)
            {
                scheduler.Stop();
                return;
            }
            if (phase == BossPhase.Intro) return;

            var wave = scheduler.Tick(Time.deltaTime, spawner.AliveCount, spawner.Cap, CountFresh());
            if (wave == null) return;
            PossessableRack rack = NextRack(wave.Value.FreshRack);
            if (rack == null) return;
            if (wave.Value.FreshRack)
            {
                rack.Possess(spawner, wave.Value.Count, RebuildNavMesh);
            }
            else
            {
                spawner.Spawn(rack.ReleasePoint, wave.Value.Count);
            }
        }

        private int CountFresh()
        {
            int n = 0;
            foreach (var rack in racks) if (rack != null && !rack.IsPossessed) n++;
            return n;
        }

        private PossessableRack NextRack(bool fresh)
        {
            if (racks.Count == 0) return null;
            if (fresh)
            {
                var candidates = racks.FindAll(r => r != null && !r.IsPossessed);
                return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
            }
            reuseIndex = (reuseIndex + 1) % racks.Count;
            return racks[reuseIndex];
        }

        private void RebuildNavMesh()
        {
            if (rig != null && rig.NavMeshSurface != null) rig.NavMeshSurface.BuildNavMesh();
        }
    }
}
