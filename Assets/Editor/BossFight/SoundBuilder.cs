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
                    changed |= SetOn(rack, ("telegraphHum", "rack_telegraph_hum"), ("telegraphCreak", "rack_telegraph_creak"), ("slamSound", "rack_slam"));
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

                var music = BuilderUtil.FindByName(scene, MusicName);
                if (music == null)
                {
                    var go = new GameObject(MusicName, typeof(AudioSource), typeof(BossMusic));
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    music = go.transform;
                    changed = true;
                }
                var source = music.GetComponent<AudioSource>();
                var loop = Clip("music_boss_loop");
                if (source.clip != loop || source.playOnAwake || !source.loop)
                {
                    source.clip = loop;
                    source.playOnAwake = false;
                    source.loop = true;
                    changed = true;
                }
                var director = BuilderUtil.FindInScene<BossDirector>(scene);
                var musicSo = new SerializedObject(music.GetComponent<BossMusic>());
                changed |= BuilderUtil.SetReference(musicSo, "director", director);
                changed |= musicSo.ApplyModifiedPropertiesWithoutUndo();

                if (changed) EditorSceneManager.MarkSceneDirty(scene);
                boss.SaveIfDirty();
            }
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
            var music = BuilderUtil.FindByName(scene, MusicName);
            if (music == null || music.GetComponent<AudioSource>().clip == null) errors.Add("Sound: Boss Scene has no BossMusic with a clip");
            var player = BuilderUtil.FindPlayer(scene);
            if (player == null || player.GetComponent<PlayerFootsteps>() == null) errors.Add("Sound: the player has no PlayerFootsteps");
        }
    }
}
