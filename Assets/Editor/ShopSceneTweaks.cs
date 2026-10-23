using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off MainScene changes approved by Enzo on 2026-10-23. Deliberately NOT a BuildAll step: MainScene is the
// team's scene and BuildAllTests checks that BuildAll leaves it alone. Safe to run again (it changes nothing twice).
// Run headless: unity run . -- -executeMethod ShopSceneTweaks.Run
public static class ShopSceneTweaks
{
    private const string MainScenePath = "Assets/Scenes/Scenes in Game/MainScene.unity";
    private const int StartingReputation = 80;
    private const string ShopMusicName = "ShopMusic";
    private const string ShopMusicClip = "Assets/Audio/Shop/shop_music_very_crunchy.wav";
    private const float ShopMusicVolume = 0.5f;

    [MenuItem("Boss Fight/Apply Shop Scene Tweaks")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        bool changed = false;

        var reputation = Object.FindFirstObjectByType<ReputationManager>();
        if (reputation == null) throw new System.InvalidOperationException("MainScene has no ReputationManager");
        var so = new SerializedObject(reputation);
        var start = so.FindProperty("startingReputation");
        if (start.intValue != StartingReputation)
        {
            start.intValue = StartingReputation;
            so.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        // The team's Wwise shop music (PLAY_SHOP_MUSIC) has no soundbanks in the repo, so it is silent. This plain
        // Unity loop stands in; remove it once the Wwise banks are committed, or both will play.
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ShopMusicClip);
        if (clip == null) throw new System.InvalidOperationException($"Missing {ShopMusicClip}");
        var importer = (AudioImporter)AssetImporter.GetAtPath(ShopMusicClip);
        var settings = importer.defaultSampleSettings;
        if (settings.loadType != AudioClipLoadType.Streaming || settings.compressionFormat != AudioCompressionFormat.Vorbis)
        {
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ShopMusicClip);
        }
        GameObject music = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == ShopMusicName) music = root;
        }
        if (music == null)
        {
            music = new GameObject(ShopMusicName, typeof(AudioSource));
            changed = true;
        }
        var source = music.GetComponent<AudioSource>();
        if (source.clip != clip || !source.loop || !source.playOnAwake || source.spatialBlend != 0f
            || !Mathf.Approximately(source.volume, ShopMusicVolume))
        {
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = ShopMusicVolume;
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"ShopSceneTweaks: {(changed ? "saved" : "no change")}");
    }
}
