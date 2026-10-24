using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // One looping music layer of the fight. It fades in when the opening starts and out when the fight ends; a layer
    // can instead wait for the first hit on the Witch (the boss theme) or stop at it (the heartbeat).
    [RequireComponent(typeof(AudioSource))]
    public class BossMusic : MonoBehaviour
    {
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private float volume = 0.3f; // SoundBuilder sets each layer's volume
        [SerializeField]
        private float fadeSeconds = 2.5f;
        [SerializeField]
        private bool startAfterFirstHit;
        [SerializeField]
        private bool stopAtFirstHit;

        private AudioSource source;
        private bool firstHit;

        public bool FirstHit => firstHit;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            if (director == null) director = FindFirstObjectByType<BossDirector>();
        }

        private void OnEnable()
        {
            if (director != null) director.IntroWitchHit += OnFirstHit;
        }

        private void OnDisable()
        {
            if (director != null) director.IntroWitchHit -= OnFirstHit;
        }

        private void OnFirstHit() => firstHit = true;

        private void Update()
        {
            if (director == null || source.clip == null) return;
            var phase = director.Phase;
            // A skipped opening never raises IntroWitchHit; leaving the Intro counts as the first hit too.
            if (phase != BossPhase.Intro) firstHit = true;
            bool fighting = director.OpeningRunning || phase == BossPhase.Phase1 || phase == BossPhase.Phase2;
            bool on = fighting && (!startAfterFirstHit || firstHit) && (!stopAtFirstHit || !firstHit);
            if (on && !source.isPlaying) source.Play();
            float target = on ? volume : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * volume / Mathf.Max(fadeSeconds, 0.01f));
            if (!on && source.isPlaying && source.volume <= 0.001f) source.Stop();
        }
    }
}
