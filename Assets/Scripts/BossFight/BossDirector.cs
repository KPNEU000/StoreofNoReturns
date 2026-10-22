using System;
using System.Collections;
using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // Drives the fight: owns the phase machine and the monster registry, and runs the win/lose outcome.
    public class BossDirector : MonoBehaviour
    {
        public const string WinSceneName = "You WIn Screen";

        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private MoneyManager moneyManager;
        [SerializeField]
        private PlayerMovement player;

        private BossPhaseMachine machine;
        private BossFightSettings settings;
        private readonly List<IDamageable> monsters = new List<IDamageable>();

        public BossPhase Phase => machine.Phase;
        public int MonstersAlive => machine.MonstersAlive;
        public float WitchHp => machine.WitchHp;
        public bool WitchDamageable => machine.WitchDamageable;

        public event Action<BossPhase> PhaseChanged;
        public event Action Won;
        public event Action Lost;
        // Raised by StartFight with the monster count; a spawner listening here spawns and calls RegisterMonster.
        public event Action<int> MonsterSpawnRequested;

        private void Awake()
        {
            settings = config != null ? config.ToSettings() : new BossFightSettings();
            machine = new BossPhaseMachine(settings);
            if (moneyManager == null) moneyManager = FindFirstObjectByType<MoneyManager>();
            if (player == null) player = FindFirstObjectByType<PlayerMovement>();
        }

        private void OnEnable()
        {
            if (moneyManager != null) moneyManager.OnMoneyChanged += HandleMoneyChanged;
        }

        private void OnDisable()
        {
            if (moneyManager != null) moneyManager.OnMoneyChanged -= HandleMoneyChanged;
        }

        // The Intro (opening sequence) is not built yet, so this goes straight into the fight.
        public bool StartFight(int monsterCount)
        {
            if (machine.Phase != BossPhase.Intro) return false;
            MonsterSpawnRequested?.Invoke(monsterCount);
            PruneMonsters();
            if (!machine.Start(monsters.Count)) return false;
            if (player != null) player.enableMovement();
            PhaseChanged?.Invoke(machine.Phase);
            return true;
        }

        public void RegisterMonster(IDamageable monster)
        {
            if (monster == null || monsters.Contains(monster)) return;
            monsters.Add(monster);
            UpdateMonsterCount();
        }

        public void MonsterDied(IDamageable monster)
        {
            monsters.Remove(monster);
            UpdateMonsterCount();
        }

        // Returns true when the damage landed (Phase2 only).
        public bool DamageWitch(float amount)
        {
            bool landed = machine.WitchDamageable && amount > 0f;
            if (machine.DamageWitch(amount)) OnPhaseChanged();
            return landed;
        }

        private void Update()
        {
            if (machine.Phase == BossPhase.Phase1) UpdateMonsterCount();
        }

        private void UpdateMonsterCount()
        {
            PruneMonsters();
            if (machine.Phase == BossPhase.Intro) return;
            if (machine.SetMonstersAlive(monsters.Count)) OnPhaseChanged();
        }

        private void PruneMonsters()
        {
            monsters.RemoveAll(m => m == null || (m is UnityEngine.Object o && o == null) || !m.IsAlive);
        }

        private void HandleMoneyChanged(float money)
        {
            if (machine.OnMoneyChanged(money)) OnPhaseChanged();
        }

        private void OnPhaseChanged()
        {
            PhaseChanged?.Invoke(machine.Phase);
            if (machine.Phase == BossPhase.Won)
            {
                RunScore.Set(moneyManager != null ? moneyManager.CurrentMoney : 0f);
                DropAllMinions();
                Won?.Invoke();
                StartCoroutine(LoadWinScene());
            }
            else if (machine.Phase == BossPhase.Lost)
            {
                // MoneyManager already loads the Game Over scene at money <= 0.
                Lost?.Invoke();
            }
        }

        private void DropAllMinions()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IBossMinion minion) minion.DropLifeless();
            }
            foreach (var spawner in FindObjectsByType<ClothingSpawner>(FindObjectsSortMode.None))
            {
                spawner.enabled = false;
            }
            // Legacy shop enemies have no IBossMinion; just switch their AI off.
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                enemy.enabled = false;
                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
            }
        }

        private IEnumerator LoadWinScene()
        {
            yield return new WaitForSecondsRealtime(settings.winPauseSeconds);
            if (SceneLoadingManager.Instance != null)
            {
                SceneLoadingManager.Instance.LoadSceneByName(WinSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(WinSceneName);
            }
        }
    }
}
