using UnityEngine;

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

        // The tutorial lets the player land exactly one hit on the Witch before the fight starts.
        public bool IntroHitOpen { get; private set; }
        private bool introHitTaken;

        public bool WitchDamageable => Phase == BossPhase.Phase2 || IntroHitOpen;
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
            IntroHitOpen = false; // the tutorial hit (if any) already came off her HP
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

        public void OpenIntroHit()
        {
            if (Phase == BossPhase.Intro && !introHitTaken) IntroHitOpen = true;
        }

        // Damage outside Phase2 (shielded, not started, or over) is ignored, except the one tutorial hit.
        // Returns true when the phase changed.
        public bool DamageWitch(float amount)
        {
            if (!WitchDamageable || amount <= 0f) return false;
            if (Phase == BossPhase.Intro)
            {
                // The tutorial hit can never kill her.
                WitchHp = Mathf.Max(1f, WitchHp - amount);
                IntroHitOpen = false;
                introHitTaken = true;
                return false;
            }
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
