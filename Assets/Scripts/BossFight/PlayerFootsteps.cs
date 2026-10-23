using UnityEngine;

namespace BossFight
{
    // A very light pitter-patter while the player walks: a random carpet step every stride, quiet and flat (2D).
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField]
        private AudioClip[] steps = new AudioClip[0];
        [SerializeField]
        private float volume = 0.22f;
        // Seconds between steps at full walking speed; slower movement spaces them out.
        [SerializeField]
        private float strideSeconds = 0.42f;

        private CharacterController controller;
        private PlayerMovement movement;
        private float untilNext;
        private int last = -1;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (steps.Length == 0 || controller == null || !controller.enabled) return;
            var v = controller.velocity;
            float speed = new Vector2(v.x, v.z).magnitude;
            float walk = movement != null ? Mathf.Max(movement.speed, 0.1f) : 7f;
            if (speed < 0.2f * walk || !controller.isGrounded)
            {
                untilNext = Mathf.Min(untilNext, 0.1f);
                return;
            }
            untilNext -= Time.deltaTime * Mathf.Clamp(speed / walk, 0.4f, 1.5f);
            if (untilNext > 0f) return;
            untilNext = strideSeconds;
            int pick = Random.Range(0, steps.Length);
            if (pick == last && steps.Length > 1) pick = (pick + 1) % steps.Length;
            last = pick;
            Sfx.Play(steps[pick], transform.position, volume, 1f, 0.1f, 0f);
        }
    }
}
