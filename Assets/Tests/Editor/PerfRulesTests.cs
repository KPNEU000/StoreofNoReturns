using System.IO;
using System.Text.RegularExpressions;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class PerfRulesTests
{
    private static readonly Regex PerFrameMethod = new Regex(@"\bvoid\s+(Update|LateUpdate|FixedUpdate)\s*\(\s*\)");
    private static readonly Regex Forbidden = new Regex(@"\b(Find\w*Object\w*|GameObject\.Find\w*|GetComponent\w*)\s*(<[^>(]*>)?\s*\(");

    // Body of the brace block opening at the first '{' after start.
    private static string BodyAt(string src, int start)
    {
        int open = src.IndexOf('{', start);
        int depth = 0;
        for (int i = open; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0) return src.Substring(open, i - open + 1);
        }
        return src.Substring(open);
    }

    [Test]
    public void NoLookupCallsInPerFrameMethods()
    {
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts/BossFight"), "*.cs", SearchOption.AllDirectories))
        {
            var src = File.ReadAllText(file);
            foreach (Match m in PerFrameMethod.Matches(src))
            {
                var body = BodyAt(src, m.Index);
                var hit = Forbidden.Match(body);
                if (hit.Success) offenders.Add(Path.GetFileName(file) + ": " + m.Groups[1].Value + " calls " + hit.Value);
            }
        }
        Assert.IsEmpty(offenders, string.Join("\n", offenders));
    }

    [Test]
    public void ScannerFlagsLookupInUpdate()
    {
        const string src = "class A { void Update() { var x = GetComponent<Rigidbody>(); } }";
        var m = PerFrameMethod.Match(src);
        Assert.IsTrue(m.Success);
        Assert.IsTrue(Forbidden.IsMatch(BodyAt(src, m.Index)));
    }

    [Test]
    public void OnlyFlashlightKeepsShadows()
    {
        var root = new GameObject("rig");
        var beamGo = new GameObject("beam");
        beamGo.transform.SetParent(root.transform);
        root.AddComponent<PlayerFlashlight>();
        var beam = beamGo.AddComponent<Light>();
        beam.shadows = LightShadows.Soft;
        var other = new GameObject("lamp").AddComponent<Light>();
        other.shadows = LightShadows.Hard;
        try
        {
            int changed = ShadowRules.RestrictToFlashlight(new[] { beam, other });
            Assert.AreEqual(1, changed);
            Assert.AreEqual(LightShadows.Soft, beam.shadows);
            Assert.AreEqual(LightShadows.None, other.shadows);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(other.gameObject);
        }
    }
}
