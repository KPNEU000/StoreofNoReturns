using NUnit.Framework;

public class SmokeTests
{
    // Proves the EditMode test pipeline compiles and runs against Assembly-CSharp.
    [Test]
    public void ProjectScriptsAreReachable()
    {
        Assert.IsNotNull(typeof(MoneyManager));
    }
}
