using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // Comes on by itself when the store lights go out (money <= $500) and stays on; slashing dims it to a faint
    // glow. Drives a Spot Light child of the camera, set back behind the eye so its falloff is even.
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
        // URP spot lights fall off with the square of distance, so a light at the eye made close surfaces glare
        // and the floor ahead dim. Moving the source back evens the pool out (playtest round 5).
        private const float BeamSetback = 1.2f;
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
            SetBackBeam();
        }

        private void Start()
        {
            money = FindFirstObjectByType<MoneyManager>();
            fraction = 0f;
            if (beam != null) beam.intensity = 0f;
        }

        private void SetBackBeam()
        {
            var eye = GetComponentInParent<Camera>();
            if (beam == null || eye == null) return;
            beam.transform.position = eye.transform.position - eye.transform.forward * BeamSetback;
            beam.transform.rotation = eye.transform.rotation;
            beam.range = settings.flashlightRange + BeamSetback;
            // The player's own body, sword and torch now sit inside the cone; they must not shadow it.
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
