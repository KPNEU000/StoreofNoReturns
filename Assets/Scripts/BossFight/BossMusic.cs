using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // The fight's music: a dark loop that fades in when the opening starts and fades out when the fight ends.
    [RequireComponent(typeof(AudioSource))]
    public class BossMusic : MonoBehaviour
    {
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private float volume = 0.25f;
        [SerializeField]
        private float fadeSeconds = 2.5f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            if (director == null) director = FindFirstObjectByType<BossDirector>();
        }

        private void Update()
        {
            if (director == null || source.clip == null) return;
            var phase = director.Phase;
            bool fighting = director.OpeningRunning || phase == BossPhase.Phase1 || phase == BossPhase.Phase2;
            if (fighting && !source.isPlaying) source.Play();
            float target = fighting ? volume : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * volume / Mathf.Max(fadeSeconds, 0.01f));
            if (!fighting && source.isPlaying && source.volume <= 0.001f) source.Stop();
        }
    }
}
