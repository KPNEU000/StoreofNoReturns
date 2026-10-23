using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // Comes on by itself when the store lights go out (money <= $500) and stays on; slashing dims it to a faint
    // glow. Drives a Spot Light that shines from the flashlight model's lens along the view.
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
            if (visual != null) restPosition = visual.localPosition;
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
            if (on) fraction = rules.Step(Mathf.Max(fraction, rules.Faint), true, PlayerSword.IsSlashing, Time.deltaTime);
            else fraction = 0f;
            if (beam != null) beam.intensity = rules.Intensity(fraction);

            if (visual != null)
            {
                float raised = Mathf.InverseLerp(rules.Faint, 1f, fraction);
                float sway = Mathf.Sin(Time.time * 1.3f) * 0.008f;
                visual.localPosition = restPosition + new Vector3(0f, sway, 0f);
                visual.localRotation = Quaternion.Euler(Vector3.Lerp(restEuler, raisedEuler, raised));
            }
        }
    }
}
