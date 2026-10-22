using UnityEngine;

namespace BossFight.Rules
{
    public enum MonsterAttackState
    {
        Idle,
        Windup,
        Cooldown,
    }

    // A monster swing: it commits to a windup, and the hit is only checked at the very end of it,
    // so a player who backs out of reach (or around behind it) makes it whiff.
    public class MonsterAttack
    {
        private readonly float windupSeconds;
        private readonly float reach;
        private readonly float coneDegrees;
        private readonly float cooldownSeconds;
        private float timer;

        public MonsterAttack(BossFightSettings settings)
        {
            windupSeconds = settings.monsterWindupSeconds;
            reach = settings.monsterAttackReach;
            coneDegrees = settings.monsterAttackConeDegrees;
            cooldownSeconds = settings.monsterAttackCooldownSeconds;
        }

        public MonsterAttackState State { get; private set; } = MonsterAttackState.Idle;
        public float WindupSeconds => windupSeconds;

        public bool InReach(Vector3 monsterPosition, Vector3 playerPosition)
        {
            var flat = playerPosition - monsterPosition;
            flat.y = 0f;
            return flat.magnitude <= reach;
        }

        public bool InFront(Vector3 monsterPosition, Vector3 monsterForward, Vector3 playerPosition)
        {
            var flat = playerPosition - monsterPosition;
            flat.y = 0f;
            monsterForward.y = 0f;
            if (flat.sqrMagnitude < 1e-6f) return true;
            if (monsterForward.sqrMagnitude < 1e-6f) return false;
            return Vector3.Angle(monsterForward, flat) <= coneDegrees * 0.5f;
        }

        // Starts a windup when idle and the player is in reach. Returns true if one began.
        public bool TryBegin(Vector3 monsterPosition, Vector3 playerPosition)
        {
            if (State != MonsterAttackState.Idle || !InReach(monsterPosition, playerPosition)) return false;
            State = MonsterAttackState.Windup;
            timer = windupSeconds;
            return true;
        }

        // Advances the swing. Returns true only on the frame the windup ends with the player still in reach and in front.
        public bool Tick(float deltaTime, Vector3 monsterPosition, Vector3 monsterForward, Vector3 playerPosition)
        {
            switch (State)
            {
                case MonsterAttackState.Windup:
                    timer -= deltaTime;
                    if (timer > 0f) return false;
                    bool hit = InReach(monsterPosition, playerPosition) && InFront(monsterPosition, monsterForward, playerPosition);
                    State = MonsterAttackState.Cooldown;
                    timer = cooldownSeconds;
                    return hit;
                case MonsterAttackState.Cooldown:
                    timer -= deltaTime;
                    if (timer <= 0f) State = MonsterAttackState.Idle;
                    return false;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            State = MonsterAttackState.Idle;
        }
    }
}
