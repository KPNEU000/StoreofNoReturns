using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // Plugs the CC0 clips in Assets/Audio/BossFight (see CREDITS.md there) into every sound slot, and adds the
    // footstep and music players. Slots that are deliberately silent (monster/Witch hurt, monster death) stay
    // empty: the player's sword hit already plays one sound for every enemy it hits.
    public static class SoundBuilder
    {
        public const string AudioFolder = "Assets/Audio/BossFight";
        internal const string MusicName = "BossMusic";
        internal const string HeartbeatName = "BossHeartbeat";
        internal const string ThemeName = "BossTheme";

        // 2026-10-23 mix: the dark ambient loop at 30% from the opening, a quiet heartbeat until the first hit on the
        // Witch, then the boss theme on top.
        private static readonly (string name, string clip, float volume, float fade, bool startAfterHit, bool stopAtHit)[] MusicLayers =
        {
            (MusicName, "music_boss_loop", 0.1125f, 2.5f, false, false),
            (HeartbeatName, "heartbeat_loop", 0.3f, 0.75f, false, true),
            (ThemeName, "boss_music", 0.3f, 1.5f, true, false),
        };

        internal static AudioClip Clip(string name)
        {
            foreach (var ext in new[] { ".ogg", ".wav" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}{ext}");
                if (clip != null) return clip;
            }
            throw new System.InvalidOperationException($"Sound '{name}' missing from {AudioFolder}");
        }

        public static void Build()
        {
            AssignPrefab();
            using (var boss = new BossSceneScope())
            {
                var scene = boss.Scene;
                bool changed = false;
                changed |= Set<PlayerSword>(scene, ("swooshSound", "sword_swing"), ("hitSound", "sword_hit"));
                changed |= Set<PlayerHitReceiver>(scene, ("hitSound", "player_hurt"));
                changed |= Set<OpeningSequence>(scene, ("magicSound", "witch_magic"), ("shatterSound", "monitor_shatter"), ("doorBlastSound", "door_blast"));
                changed |= Set<WitchBookAttack>(scene, ("castSound", "witch_book_cast"));
                changed |= Set<WitchBoss>(scene, ("shatterSound", "witch_shield_shatter"));
                changed |= Set<CaveInController>(scene, ("crackSound", "ceiling_crack"), ("rumbleSound", "ceiling_rumble"), ("impactSound", "ceiling_chunk_land"));
                foreach (var book in BuilderUtil.FindAllInScene<ThrowableBook>(scene))
                {
                    changed |= SetOn(book, ("whooshSound", "book_whoosh"), ("impactSound", "book_land"));
                }
                foreach (var rack in BuilderUtil.FindAllInScene<PossessableRack>(scene))
                {
                    changed |= SetOn(rack, ("telegraphHum", "rack_telegraph_hum"), ("slamSound", "rack_slam"));
                    // The creak (it sounded like a squeaky door) was removed in the final playtest.
                    var rackSo = new SerializedObject(rack);
                    changed |= BuilderUtil.SetReference(rackSo, "telegraphCreak", null);
                    changed |= rackSo.ApplyModifiedPropertiesWithoutUndo();
                }
                var spawner = BuilderUtil.FindInScene<MonsterSpawner>(scene);
                if (spawner != null) changed |= SetArray(spawner, "screamSounds", Clip("monster_scream_1"), Clip("monster_scream_2"), Clip("monster_scream_3"));

                var player = BuilderUtil.FindPlayer(scene);
                if (player != null)
                {
                    var steps = player.GetComponent<PlayerFootsteps>();
                    if (steps == null) { steps = player.gameObject.AddComponent<PlayerFootsteps>(); changed = true; }
                    var clips = new AudioClip[5];
                    for (int i = 0; i < clips.Length; i++) clips[i] = Clip($"footstep_carpet_{i}");
                    changed |= SetArray(steps, "steps", clips);
                }

                var director = BuilderUtil.FindInScene<BossDirector>(scene);
                foreach (var layer in MusicLayers) changed |= EnsureMusic(scene, director, layer);
                changed |= EnsurePlayerListener(scene);

                if (changed) EditorSceneManager.MarkSceneDirty(scene);
                boss.SaveIfDirty();
            }
        }

        private static bool EnsureMusic(UnityEngine.SceneManagement.Scene scene, BossDirector director,
            (string name, string clip, float volume, float fade, bool startAfterHit, bool stopAtHit) layer)
        {
            bool changed = false;
            var music = BuilderUtil.FindByName(scene, layer.name);
            if (music == null)
            {
                var go = new GameObject(layer.name, typeof(AudioSource), typeof(BossMusic));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                music = go.transform;
                changed = true;
            }
            var source = music.GetComponent<AudioSource>();
            var clip = Clip(layer.clip);
            StreamMusic(clip);
            if (source.clip != clip || source.playOnAwake || !source.loop)
            {
                source.clip = clip;
                source.playOnAwake = false;
                source.loop = true;
                changed = true;
            }
            var so = new SerializedObject(music.GetComponent<BossMusic>());
            changed |= SetFloat(so, "volume", layer.volume);
            changed |= SetFloat(so, "fadeSeconds", layer.fade);
            changed |= SetBool(so, "startAfterFirstHit", layer.startAfterHit);
            changed |= SetBool(so, "stopAtFirstHit", layer.stopAtHit);
            changed |= BuilderUtil.SetReference(so, "director", director);
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        // Music tracks stream as Vorbis instead of sitting decompressed in memory (boss_music.wav alone is 41 s).
        internal static void StreamMusic(AudioClip clip)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            if (importer == null) return;
            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming && settings.compressionFormat == AudioCompressionFormat.Vorbis) return;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static bool SetFloat(SerializedObject so, string field, float value)
        {
            var prop = so.FindProperty(field);
            if (Mathf.Approximately(prop.floatValue, value)) return false;
            prop.floatValue = value;
            return true;
        }

        private static bool SetBool(SerializedObject so, string field, bool value)
        {
            var prop = so.FindProperty(field);
            if (prop.boolValue == value) return false;
            prop.boolValue = value;
            return true;
        }

        // The team's Wwise setup replaced the player camera's AudioListener with Wwise's own listener; the fight's
        // sounds are Unity AudioSources, so the camera holding the sword needs a Unity listener as well.
        // ShopShutdown switches the shop cameras' listeners off.
        private static bool EnsurePlayerListener(UnityEngine.SceneManagement.Scene scene)
        {
            var sword = BuilderUtil.FindInScene<PlayerSword>(scene);
            var camera = sword != null ? sword.GetComponentInParent<Camera>(true) : null;
            if (camera == null) throw new System.InvalidOperationException("Boss Scene has no camera holding the PlayerSword");
            var listener = camera.GetComponent<AudioListener>();
            if (listener == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
                return true;
            }
            if (listener.enabled) return false;
            listener.enabled = true;
            return true;
        }

        private static void AssignPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterEnemyBuilder.MonsterPrefab);
            var monster = prefab != null ? prefab.GetComponent<MonsterCustomer>() : null;
            if (monster == null) return;
            if (SetOn(monster, ("windupSound", "monster_windup_growl")))
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
            }
        }

        private static bool Set<T>(UnityEngine.SceneManagement.Scene scene, params (string field, string clip)[] pairs) where T : Component
        {
            var component = BuilderUtil.FindInScene<T>(scene);
            return component != null && SetOn(component, pairs);
        }

        private static bool SetOn(Component component, params (string field, string clip)[] pairs)
        {
            var so = new SerializedObject(component);
            bool changed = false;
            foreach (var (field, clip) in pairs) changed |= BuilderUtil.SetReference(so, field, Clip(clip));
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static bool SetArray(Component component, string field, params AudioClip[] clips)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            bool same = prop.arraySize == clips.Length;
            for (int i = 0; same && i < clips.Length; i++) same = prop.GetArrayElementAtIndex(i).objectReferenceValue == clips[i];
            if (same) return false;
            prop.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            void Need(Component c, string label, params string[] fields)
            {
                if (c == null) { errors.Add($"Sound: no {label} in Boss Scene"); return; }
                var so = new SerializedObject(c);
                foreach (var f in fields)
                {
                    if (so.FindProperty(f)?.objectReferenceValue == null) errors.Add($"Sound: {label}.{f} has no clip");
                }
            }
            Need(BuilderUtil.FindInScene<PlayerSword>(scene), "PlayerSword", "swooshSound", "hitSound");
            Need(BuilderUtil.FindInScene<PlayerHitReceiver>(scene), "PlayerHitReceiver", "hitSound");
            Need(BuilderUtil.FindInScene<WitchBookAttack>(scene), "WitchBookAttack", "castSound");
            foreach (var rack in BuilderUtil.FindAllInScene<PossessableRack>(scene)) Need(rack, rack.name, "telegraphHum", "slamSound");
            foreach (var layer in MusicLayers)
            {
                var music = BuilderUtil.FindByName(scene, layer.name);
                var source = music != null ? music.GetComponent<AudioSource>() : null;
                if (source == null || source.clip == null) errors.Add($"Sound: Boss Scene has no {layer.name} with a clip");
            }
            var listeners = new List<AudioListener>();
            foreach (var root in scene.GetRootGameObjects()) listeners.AddRange(root.GetComponentsInChildren<AudioListener>(true));
            var enabled = listeners.FindAll(l => l.enabled && l.gameObject.activeInHierarchy);
            if (enabled.Count != 1 || !ShopShutdown.IsPlayerListener(enabled[0]))
            {
                errors.Add($"Sound: Boss Scene needs exactly one enabled AudioListener, on the player camera (found {enabled.Count})");
            }
            var player = BuilderUtil.FindPlayer(scene);
            if (player == null || player.GetComponent<PlayerFootsteps>() == null) errors.Add("Sound: the player has no PlayerFootsteps");
        }
    }
}
