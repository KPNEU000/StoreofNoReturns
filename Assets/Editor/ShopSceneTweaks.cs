using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off MainScene changes approved by Enzo on 2026-10-23 (balance pass 2 the same day: money 1350, reputation 50,
// every reputation change doubled, the Tailor's dress costs $200 instead of paying out). Deliberately NOT a BuildAll step: MainScene is the
// team's scene and BuildAllTests checks that BuildAll leaves it alone. Safe to run again (it changes nothing twice).
// Run headless: unity run . -- -executeMethod ShopSceneTweaks.Run
public static class ShopSceneTweaks
{
    private const string MainScenePath = "Assets/Scenes/Scenes in Game/MainScene.unity";
    private const int StartingReputation = 50;
    private const float StartingMoney = 1350f;
    private const int CorrectRefundReward = 10;
    private const int UnfairDenialPenalty = 20;
    private const int WrongReasonAmount = -4;
    private const string CustomerPrefab = "Assets/Prefabs/CC Attribution Characters/Character.prefab";
    private const string DressCostume = "Assets/Scripts/ScriptableObjects/DressCostume.asset";
    private const float DressReturnValue = 200f;
    private const string ShopMusicName = "ShopMusic";
    private const string ShopMusicClip = "Assets/Audio/Shop/shop_music_very_crunchy.wav";
    private const float ShopMusicVolume = 0.5f;

    [MenuItem("Boss Fight/Apply Shop Scene Tweaks")]
    public static void Run()
    {
        TweakTitleScreen();
        TweakCredits();
        TweakAssets();
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        bool changed = false;

        var reputation = Object.FindFirstObjectByType<ReputationManager>();
        if (reputation == null) throw new System.InvalidOperationException("MainScene has no ReputationManager");
        changed |= SetInt(reputation, "startingReputation", StartingReputation);
        changed |= SetInt(reputation, "correctRefundReward", CorrectRefundReward);
        changed |= SetInt(reputation, "unfairDenialPenalty", UnfairDenialPenalty);
        var money = Object.FindFirstObjectByType<MoneyManager>();
        if (money == null) throw new System.InvalidOperationException("MainScene has no MoneyManager");
        changed |= SetFloat(money, "startingMoney", StartingMoney);
        // Customers outside the prefab (the Witch) carry their own copy; prefab instances follow the prefab.
        foreach (var customer in Object.FindObjectsByType<CustomerScript>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(customer)) changed |= SetInt(customer, "wrongReasonAmount", WrongReasonAmount);
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

    // The title art and its Play/Quit buttons sit on "renderedCanvas", which used Constant Pixel Size: an 835x479 px
    // picture, a small box on a large Game view (2026-10-23 playtest). Scaling with the screen fills it instead;
    // the buttons scale with the picture, so clicks still line up.
    private const string TitleScenePath = "Assets/Scenes/Scenes in Game/Title Screen.unity";
    private static readonly Vector2 TitleReference = new Vector2(835f, 479f);

    private static void TweakTitleScreen()
    {
        var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        UnityEngine.UI.CanvasScaler scaler = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "renderedCanvas") scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        }
        if (scaler == null) throw new System.InvalidOperationException("Title Screen has no renderedCanvas CanvasScaler");
        if (scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.referenceResolution == TitleReference && Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f)) return;
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = TitleReference;
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(scaler);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Enzo's surname is Markarian (the credits had "Marcarian").
    private static readonly string[] CreditsScenePaths =
    {
        "Assets/Scenes/Scenes in Game/Just Credits.unity",
        "Assets/Scenes/Scenes in Game/You WIn Screen.unity",
    };

    private static void TweakCredits()
    {
        foreach (var path in CreditsScenePaths) TweakCredits(path);
    }

    private static void TweakCredits(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        bool changed = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                var so = new SerializedObject(behaviour);
                var list = so.FindProperty("credits");
                if (list == null || !list.isArray) continue;
                for (int i = 0; i < list.arraySize; i++)
                {
                    var line = list.GetArrayElementAtIndex(i);
                    if (line.propertyType != SerializedPropertyType.String || !line.stringValue.Contains("Enzo Marcarian")) continue;
                    line.stringValue = line.stringValue.Replace("Enzo Marcarian", "Enzo Markarian");
                    changed = true;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void TweakAssets()
    {
        var dress = AssetDatabase.LoadAssetAtPath<CostumeScriptData>(DressCostume);
        if (dress == null) throw new System.InvalidOperationException($"Missing {DressCostume}");
        if (SetFloat(dress, "returnValue", DressReturnValue)) AssetDatabase.SaveAssetIfDirty(dress);

        var root = PrefabUtility.LoadPrefabContents(CustomerPrefab);
        try
        {
            var customer = root.GetComponent<CustomerScript>();
            if (customer == null) throw new System.InvalidOperationException($"{CustomerPrefab} has no CustomerScript");
            if (SetInt(customer, "wrongReasonAmount", WrongReasonAmount)) PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop.intValue == value) return false;
        prop.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }

    private static bool SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (Mathf.Approximately(prop.floatValue, value)) return false;
        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }
}
