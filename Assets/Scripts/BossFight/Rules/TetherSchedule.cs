namespace BossFight.Rules
{
    // The Witch's tether in Phase1: every interval seconds she links to one monster for a few seconds.
    public class TetherSchedule
    {
        private readonly float intervalSeconds;
        private readonly float durationSeconds;
        private float timer;

        public TetherSchedule(BossFightSettings settings)
        {
            intervalSeconds = settings.tetherIntervalSeconds;
            durationSeconds = settings.tetherDurationSeconds;
            timer = intervalSeconds;
        }

        public bool Active { get; private set; }

        // Returns +1 on the frame a tether starts, -1 on the frame it ends, otherwise 0.
        public int Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return 0;
            if (Active)
            {
                Active = false;
                timer = intervalSeconds;
                return -1;
            }
            Active = true;
            timer = durationSeconds;
            return 1;
        }

        public void Reset()
        {
            Active = false;
            timer = intervalSeconds;
        }
    }
}
