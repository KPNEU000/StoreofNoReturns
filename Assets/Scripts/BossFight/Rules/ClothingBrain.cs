namespace BossFight.Rules
{
    public enum ClothingState
    {
        Chase,
        Lunge,
        Flopped,
        Rising,
        Dead,
    }

    // Pure state machine for a possessed costume: Chase -> Lunge -> Flopped -> Rising -> Chase.
    public class ClothingBrain
    {
        private readonly float flopSeconds;
        private readonly float riseSeconds;
        private float stateTimer;

        public ClothingBrain(BossFightSettings settings, float lungeSeconds = 0.3f, float riseSeconds = 0.5f)
        {
            Health = settings.clothingHp;
            flopSeconds = settings.clothingFlopSeconds;
            this.riseSeconds = riseSeconds;
            LungeSeconds = lungeSeconds;
        }

        public ClothingState State { get; private set; } = ClothingState.Chase;
        public float Health { get; private set; }
        public float LungeSeconds { get; }
        public bool IsAlive => State != ClothingState.Dead;
        public bool CanDealHit => State == ClothingState.Lunge;

        // Contact with the player while chasing starts a lunge; ignored in every other state.
        public bool OnContact()
        {
            if (State != ClothingState.Chase) return false;
            Enter(ClothingState.Lunge);
            return true;
        }

        public void TakeDamage(float amount)
        {
            if (State == ClothingState.Dead) return;
            Health -= amount;
            if (Health <= 0f)
            {
                Health = 0f;
                State = ClothingState.Dead;
            }
        }

        public void Tick(float deltaTime)
        {
            stateTimer += deltaTime;
            switch (State)
            {
                case ClothingState.Lunge:
                    if (stateTimer >= LungeSeconds) Enter(ClothingState.Flopped);
                    break;
                case ClothingState.Flopped:
                    if (stateTimer >= flopSeconds) Enter(ClothingState.Rising);
                    break;
                case ClothingState.Rising:
                    if (stateTimer >= riseSeconds) Enter(ClothingState.Chase);
                    break;
            }
        }

        private void Enter(ClothingState next)
        {
            State = next;
            stateTimer = 0f;
        }
    }
}
