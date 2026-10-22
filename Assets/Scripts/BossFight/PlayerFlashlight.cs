using BossFight.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight
{
    // Held right mouse raises the beam; a faint glow is always on. Drives a Spot Light child of the camera.
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
        private Vector3 restPosition;

        // Tutorial sets this above 1 for the extra bright beam.
        public float BrightMultiplier
        {
            get => rules != null ? rules.BrightMultiplier : 1f;
            set { if (rules != null) rules.BrightMultiplier = value; }
        }

        public float Fraction => fraction;

        private void Awake()
        {
            settings = config != null ? config.ToSettings() : new BossFightSettings();
            rules = new FlashlightRules(settings);
            fraction = rules.Faint;
            if (beam != null)
            {
                beam.type = LightType.Spot;
                beam.range = settings.flashlightRange;
                beam.spotAngle = settings.flashlightSpotAngle;
            }
            if (visual != null) restPosition = visual.localPosition;
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
            var mouse = Mouse.current;
            bool held = mouse != null && mouse.rightButton.isPressed;
            fraction = rules.Step(fraction, held, PlayerSword.IsSlashing, Time.deltaTime);
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
