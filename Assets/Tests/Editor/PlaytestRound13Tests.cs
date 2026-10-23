using System.Collections.Generic;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

// Round 13: the slash is a flat fan with no pierce cap.
public class PlaytestRound13Tests
{
    [Test]
    public void TheSlashHitsEveryEnemyInTheFan()
    {
        var rules = new SlashRules(new BossFightSettings());
        var targets = new List<Vector3>();
        for (int i = 0; i < 8; i++) targets.Add(new Vector3(-0.8f + 0.2f * i, 1f, 1.2f));
        Assert.AreEqual(8, rules.SelectTargets(Vector3.zero, Vector3.forward, targets).Count, "no pierce cap");
    }

    [Test]
    public void HeightAndLookingUpOrDownDoNotChangeTheFan()
    {
        var s = new BossFightSettings();
        var rules = new SlashRules(s);
        var targets = new List<Vector3> { new Vector3(0f, -1.5f, s.slashReach - 0.05f), new Vector3(0f, 2f, s.slashReach - 0.05f) };
        var lookingDown = new Vector3(0f, -0.9f, 0.3f);
        Assert.AreEqual(2, rules.SelectTargets(Vector3.zero, lookingDown, targets).Count);
    }

    [Test]
    public void TheSavedConfigHasNoPierceCapEither()
    {
        // The game reads the config asset, not the code defaults; a capped asset would keep the old limit of 3.
        BossFight.Editor.ConfigBuilder.Build();
        var settings = BossFight.Editor.ConfigBuilder.Load().ToSettings();
        Assert.GreaterOrEqual(settings.slashMaxTargets, 100);
    }
}
