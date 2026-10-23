using UnityEngine;

namespace BossFight
{
    // One-shot sound effects for the boss fight: a throwaway AudioSource per sound, with a little random pitch so
    // repeated sounds never sound mechanical, and 3D falloff (spatial 1) unless asked to be flat (spatial 0).
    public static class Sfx
    {
        public const float DefaultPitchJitter = 0.08f;
        // Final playtest: every effect 25% louder (callers that must not change divide it back out).
        public const float Master = 1.25f;

        public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
            float pitchJitter = DefaultPitchJitter, float spatial = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("Sfx " + clip.name);
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * Master);
            source.pitch = Mathf.Max(0.1f, pitch + Random.Range(-pitchJitter, pitchJitter));
            source.spatialBlend = spatial;
            source.minDistance = 3f;
            source.maxDistance = 40f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.playOnAwake = false;
            source.Play();
            Object.Destroy(go, clip.length / source.pitch + 0.1f);
        }
    }
}
