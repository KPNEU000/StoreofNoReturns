using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // Stays in the player's pocket while the store lights are on. When they go out (money <= $500) it is pulled up
    // into view and switched on, and stays on at full brightness, even mid-swing. Drives a Spot Light that shines
    // from the flashlight model's lens along the view.
    public class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Light beam;
        [SerializeField]
        private Transform visual;
        [SerializeField]
        private Vector3 restEuler = new Vector3(0f, 0f, 0f);
        [SerializeField]
        private Vector3 raisedEuler = new Vector3(-8f, 0f, 0f);

        // True while a boss flashlight exists; FlashlightControl's F toggle checks this.
        public static int ActiveCount { get; private set; }

        private FlashlightRules rules;
        private BossFightSettings settings;
        private float fraction;
        private bool on;
        private MoneyManager money;
        // How far in front of the flashlight model's centre its lens is.
        private const float LensOffset = 0.15f;
        // Pulled up from below the view into the hand (and back down into the pocket).
        private const float PocketDrop = 0.45f;
        private const float PullSeconds = 0.45f;
        // 0 = in the pocket (hidden), 1 = in hand.
        private float drawn;
        private Renderer[] visualRenderers;
        private Vector3 restPosition;

        // Tutorial sets this above 1 for the extra bright beam.
        public float BrightMultiplier
        {
            get => rules != null ? rules.BrightMultiplier : 1f;
            set { if (rules != null) rules.BrightMultiplier = value; }
        }

        public float Fraction => fraction;
        public bool IsOn => on;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            rules = new FlashlightRules(settings);
            fraction = rules.Faint;
            if (beam != null)
            {
                beam.type = LightType.Spot;
                beam.range = settings.flashlightRange;
                beam.spotAngle = settings.flashlightSpotAngle;
                beam.innerSpotAngle = Mathf.Min(settings.flashlightInnerSpotAngle, settings.flashlightSpotAngle);
            }
            if (visual != null)
            {
                restPosition = visual.localPosition;
                visualRenderers = visual.GetComponentsInChildren<Renderer>(true);
            }
            AimFromLens();
        }

        private void Start()
        {
            money = FindFirstObjectByType<MoneyManager>();
            fraction = 0f;
            if (beam != null) beam.intensity = 0f;
        }

        // Playtest round 6: a source set back behind the eye lit up the sword instead of coming out of the torch.
        private void AimFromLens()
        {
            var eye = GetComponentInParent<Camera>();
            if (beam == null || eye == null) return;
            var origin = visual != null ? visual.position : transform.position;
            beam.transform.position = origin + eye.transform.forward * LensOffset;
            beam.transform.rotation = eye.transform.rotation;
            beam.range = settings.flashlightRange;
            // The player's own body, sword and torch must never shadow the beam.
            var root = eye.transform.root;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void OnEnable()
        {
            ActiveCount++;
        }

        private void OnDisable()
        {
            ActiveCount--;
        }

        private void Update()
        {
            on = money != null && rules.ShouldBeOn(money.CurrentMoney);
            drawn = Mathf.MoveTowards(drawn, on ? 1f : 0f, Time.deltaTime / PullSeconds);
            // The beam only comes on once the torch is fully in hand.
            // Round 11: the beam stays at full brightness through a swing.
            if (on && drawn >= 1f) fraction = rules.Step(Mathf.Max(fraction, rules.Faint), true, false, Time.deltaTime);
            else fraction = 0f;
            if (beam != null) beam.intensity = rules.Intensity(fraction);

            if (visual != null)
            {
                bool visible = drawn > 0.001f;
                if (visualRenderers != null)
                {
                    foreach (var r in visualRenderers) if (r != null && r.enabled != visible) r.enabled = visible;
                }
                float raised = Mathf.InverseLerp(rules.Faint, 1f, fraction);
                float sway = Mathf.Sin(Time.time * 1.3f) * 0.008f;
                float eased = drawn * drawn * (3f - 2f * drawn);
                visual.localPosition = restPosition + new Vector3(0f, sway - PocketDrop * (1f - eased), 0f);
                visual.localRotation = Quaternion.Euler(Vector3.Lerp(restEuler, raisedEuler, raised));
            }
        }
    }
}
