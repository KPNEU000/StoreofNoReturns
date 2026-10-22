using System.IO;
using System.Linq;
using BossFight.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class BuildAllTests
{
    private SceneSetup[] previousSetup;

    [SetUp]
    public void SaveSceneSetup()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
    }

    [TearDown]
    public void RestoreSceneSetup()
    {
        // An untitled scene has no path and cannot be restored by path; fall back to a fresh empty scene.
        if (previousSetup.Length > 0 && previousSetup.All(s => !string.IsNullOrEmpty(s.path)))
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
        else if (SceneManager.sceneCount > 1)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    [Test]
    public void RunningTwiceIsIdempotent()
    {
        string mainSceneBefore = Hash(BossFightPaths.MainScene);

        BuildAll.Run();
        string[] filesAfterFirst = Snapshot();
        string bossSceneAfterFirst = Hash(BossFightPaths.BossScene);
        string prefabAfterFirst = Hash(BossFightPaths.RigPrefab);

        BuildAll.Run();

        CollectionAssert.AreEqual(filesAfterFirst, Snapshot(), "BuildAll created or removed files on its second run");
        Assert.AreEqual(bossSceneAfterFirst, Hash(BossFightPaths.BossScene), "Boss Scene changed on the second run");
        Assert.AreEqual(prefabAfterFirst, Hash(BossFightPaths.RigPrefab), "Rig prefab changed on the second run");
        Assert.AreEqual(mainSceneBefore, Hash(BossFightPaths.MainScene), "BuildAll touched MainScene");
    }

    [Test]
    public void BossSceneHasExactlyOneRig()
    {
        BuildAll.Run();
        using (var boss = new BossSceneScope())
        {
            Assert.AreEqual(1, BossSceneRigBuilder.FindRigs(boss.Scene).Count);
        }
    }

    [Test]
    public void ValidatorPassesAfterBuild()
    {
        BuildAll.Run();
        Assert.DoesNotThrow(BossRigValidator.Validate);
    }

    [Test]
    public void ConfigCarriesContextDefaults()
    {
        BuildAll.Run();
        var settings = ConfigBuilder.Load().ToSettings();
        Assert.AreEqual(25f, settings.clothingHitCost);
        Assert.AreEqual(50f, settings.bookHitCost);
        Assert.AreEqual(100f, settings.witchHp);
        Assert.AreEqual(5, settings.caveInChunkCount);
        Assert.AreEqual(250f, settings.caveInMoney);
    }

    [Test]
    public void BuildSettingsListTheFiveGameScenes()
    {
        BuildAll.Run();
        Assert.IsTrue(BuildSettingsBuilder.Matches());
        Assert.AreEqual(5, UnityEditor.EditorBuildSettings.scenes.Length);
    }

    private static string[] Snapshot()
    {
        return Directory.GetFiles(BossFightPaths.PrefabsFolder, "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(BossFightPaths.SettingsFolder, "*", SearchOption.AllDirectories))
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => p)
            .ToArray();
    }

    private static string Hash(string path)
    {
        using (var sha = System.Security.Cryptography.SHA1.Create())
        {
            return System.BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
