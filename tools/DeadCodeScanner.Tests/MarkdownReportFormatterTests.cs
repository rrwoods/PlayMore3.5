using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeadCodeScanner.Tests;

[TestClass]
public class MarkdownReportFormatterTests {
    [TestMethod]
    public void Format_WithZombieIslands_GeneratesDetailedMarkdown() {
        var root = new MethodNode("PlayMore35.Zombie.Root()", "Root", "PlayMore35.Zombie", "Zombie.cs", 10, false);
        var child = new MethodNode("PlayMore35.Zombie.Child()", "Child", "PlayMore35.Zombie", "Zombie.cs", 20, false);
        var test = new MethodNode("PlayMore35.Tests.ZombieTests.TestRoot()", "TestRoot", "PlayMore35.Tests.ZombieTests", "ZombieTests.cs", 15, false);

        var island = new ZombieIsland(
            "PlayMore35.Zombie",
            new[] { root },
            new[] { child },
            new[] { test }
        );

        var liveMethod = new MethodNode("PlayMore35.Program.Main()", "Main", "PlayMore35.Program", "Program.cs", 5, true);

        var result = new AnalysisResult(
            new[] { liveMethod },
            new[] { root, child },
            new[] { island },
            Array.Empty<MethodNode>()
        );

        var formatter = new MarkdownReportFormatter();
        string markdown = formatter.Format(result);

        StringAssert.Contains(markdown, "# Dead Code & Zombie Island Report");
        StringAssert.Contains(markdown, "### `PlayMore35.Zombie`");
        Assert.DoesNotContain("### Island 1", markdown, "Report should not contain ordinal island numbers.");
        StringAssert.Contains(markdown, "Root");
        StringAssert.Contains(markdown, "Child");
        StringAssert.Contains(markdown, "TestRoot");
        StringAssert.Contains(markdown, "ZombieTests.cs");
    }

    [TestMethod]
    public void Format_WithoutZombieIslands_StillIncludesIsolatedDeadMethods() {
        var isolated = new MethodNode("PlayMore35.Lonely.DoWork()", "DoWork", "PlayMore35.Lonely", "Lonely.cs", 10, false);
        var liveMethod = new MethodNode("PlayMore35.Program.Main()", "Main", "PlayMore35.Program", "Program.cs", 5, true);

        var result = new AnalysisResult(
            new[] { liveMethod },
            new[] { isolated },
            Array.Empty<ZombieIsland>(),
            new[] { isolated }
        );

        var formatter = new MarkdownReportFormatter();
        string markdown = formatter.Format(result);

        StringAssert.Contains(markdown, "No zombie islands detected!");
        StringAssert.Contains(markdown, "## Isolated Dead Methods");
        StringAssert.Contains(markdown, "Lonely.DoWork");
    }
}
