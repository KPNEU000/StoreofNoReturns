namespace BossFight.Rules
{
    public enum BossPhase
    {
        Intro,
        Phase1,
        Phase2,
        Won,
        Lost
    }

    // Fight state: Intro -> Phase1 (monsters alive) -> Phase2 (no monsters) -> Won / Lost.
    // Every method returns true when the phase changed so the caller can raise its event.
    public class BossPhaseMachine
    {
        private readonly float witchMaxHp;

        public BossPhase Phase { get; private set; } = BossPhase.Intro;
        public int MonstersAlive { get; private set; }
        public float WitchHp { get; private set; }

        public bool WitchDamageable => Phase == BossPhase.Phase2;
        public bool IsOver => Phase == BossPhase.Won || Phase == BossPhase.Lost;

        public BossPhaseMachine(BossFightSettings settings)
        {
            witchMaxHp = settings.witchHp;
            WitchHp = witchMaxHp;
        }

        // Only leaves Intro; Phase1 is skipped when there are no monsters to kill.
        public bool Start(int monsterCount)
        {
            if (Phase != BossPhase.Intro) return false;
            MonstersAlive = monsterCount > 0 ? monsterCount : 0;
            WitchHp = witchMaxHp;
            Phase = MonstersAlive > 0 ? BossPhase.Phase1 : BossPhase.Phase2;
            return true;
        }

        public bool SetMonstersAlive(int count)
        {
            MonstersAlive = count > 0 ? count : 0;
            if (Phase == BossPhase.Phase1 && MonstersAlive == 0)
            {
                Phase = BossPhase.Phase2;
                return true;
            }
            return false;
        }

        // Damage outside Phase2 (shielded, not started, or over) is ignored.
        public bool DamageWitch(float amount)
        {
            if (!WitchDamageable || amount <= 0f) return false;
            WitchHp -= amount;
            if (WitchHp <= 0f)
            {
                WitchHp = 0f;
                Phase = BossPhase.Won;
                return true;
            }
            return false;
        }

        public bool OnMoneyChanged(float money)
        {
            if (IsOver || money > 0f) return false;
            Phase = BossPhase.Lost;
            return true;
        }
    }
}
