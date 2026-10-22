namespace BossFight.Rules
{
    // A hit is accepted only if at least `seconds` have passed since the last accepted hit.
    public class InvincibilityTimer
    {
        private readonly float seconds;
        private bool hasAccepted;
        private float lastAcceptedTime;

        public InvincibilityTimer(float seconds)
        {
            this.seconds = seconds;
        }

        public bool TryAccept(float now)
        {
            if (hasAccepted && now - lastAcceptedTime < seconds)
            {
                return false;
            }
            hasAccepted = true;
            lastAcceptedTime = now;
            return true;
        }

        public void Reset()
        {
            hasAccepted = false;
        }
    }
}
