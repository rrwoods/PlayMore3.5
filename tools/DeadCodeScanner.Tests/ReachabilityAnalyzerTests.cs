using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeadCodeScanner.Tests;

[TestClass]
public class ReachabilityAnalyzerTests {
    [TestMethod]
    public void Analyze_MethodCalledFromMain_IsLive_UncalledMethod_IsDead() {
        string prodSource = @"
namespace PlayMore35;
public class Program {
    public static void Main() {
        LiveHelper();
    }
    public static void LiveHelper() {}
    public static void DeadHelper() {}
}
";
        var prodCompilation = TestCompilationHelper.CreateCompilation("PlayMore35", prodSource);
        var testCompilation = TestCompilationHelper.CreateCompilation("PlayMore35.Tests", "// empty");

        var analyzer = new ReachabilityAnalyzer();
        var result = analyzer.Analyze(prodCompilation, testCompilation);

        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "LiveHelper"), "LiveHelper should be marked Live");
        Assert.IsTrue(result.DeadMethods.Any(m => m.Name == "DeadHelper"), "DeadHelper should be marked Dead");
    }

    [TestMethod]
    public void Analyze_DeadMethodClusterCalledByTests_IdentifiesZombieIslandWithLifeSupportTests() {
        string prodSource = @"
namespace PlayMore35;
public class Program {
    public static void Main() {}
}
public class ZombieService {
    public static void ZombieRoot() {
        ZombieChild();
    }
    public static void ZombieChild() {}
}
";
        var prodCompilation = TestCompilationHelper.CreateCompilation("PlayMore35", prodSource);
        var prodRef = prodCompilation.ToMetadataReference();

        string testSource = @"
namespace PlayMore35.Tests;
public class ServiceTests {
    public void TestZombieRoot() {
        PlayMore35.ZombieService.ZombieRoot();
    }
}
";
        var testCompilation = TestCompilationHelper.CreateCompilation("PlayMore35.Tests", testSource, new[] { prodRef });

        var analyzer = new ReachabilityAnalyzer();
        var result = analyzer.Analyze(prodCompilation, testCompilation);

        Assert.HasCount(1, result.ZombieIslands, "Should detect 1 zombie island");
        var island = result.ZombieIslands[0];
        Assert.IsTrue(island.Roots.Any(r => r.Name == "ZombieRoot"), "ZombieRoot should be the island root");
        Assert.IsTrue(island.Cascade.Any(c => c.Name == "ZombieChild"), "ZombieChild should be in cascade");
        Assert.IsTrue(island.LifeSupportTests.Any(t => t.Name == "TestZombieRoot"), "TestZombieRoot should be listed in life support");
    }

    [TestMethod]
    public void Analyze_EventSubscriptionAndOverrides_RecognizedAsTrueRoots() {
        string prodSource = @"
using System;

namespace PlayMore35;

public class MockForm {
    public MockForm() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        WireEvent(OnButtonClicked);
    }

    private void WireEvent(Action action) {
        action();
    }

    private void OnButtonClicked() {
        LiveFromEventHandler();
    }

    public static void LiveFromEventHandler() {}
}
";
        var prodCompilation = TestCompilationHelper.CreateCompilation("PlayMore35", prodSource);
        var testCompilation = TestCompilationHelper.CreateCompilation("PlayMore35.Tests", "// empty");

        var analyzer = new ReachabilityAnalyzer();
        var result = analyzer.Analyze(prodCompilation, testCompilation);

        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "LiveFromEventHandler"), "Method called from event handler should be Live");
    }

    [TestMethod]
    public void Analyze_InterfaceCallAndPropertyAccess_ResolvesImplementationAsLive() {
        string prodSource = @"
namespace PlayMore35;

public interface IService {
    string Name { get; }
    void DoWork();
}

public class RealService : IService {
    public string Name => ""Real"";
    public void DoWork() {
        LiveHelper();
    }
    private void LiveHelper() {}
}

public class Program {
    public static void Main() {
        IService s = new RealService();
        var n = s.Name;
        s.DoWork();
    }
}
";
        var prodCompilation = TestCompilationHelper.CreateCompilation("PlayMore35", prodSource);
        var testCompilation = TestCompilationHelper.CreateCompilation("PlayMore35.Tests", "// empty");

        var analyzer = new ReachabilityAnalyzer();
        var result = analyzer.Analyze(prodCompilation, testCompilation);

        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "DoWork"), "RealService.DoWork should be Live");
        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "LiveHelper"), "LiveHelper should be Live");
        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "get_Name"), "get_Name should be Live");
    }

    [TestMethod]
    public void Analyze_IsolatedDeadMethodWithoutTests_IsNotClassifiedAsZombieIsland() {
        string prodSource = @"
namespace PlayMore35;
public class Program {
    public static void Main() {}
}
public class LonelyService {
    public static void CompletelyUnused() {}
}
";
        var prodCompilation = TestCompilationHelper.CreateCompilation("PlayMore35", prodSource);
        var testCompilation = TestCompilationHelper.CreateCompilation("PlayMore35.Tests", "// empty");

        var analyzer = new ReachabilityAnalyzer();
        var result = analyzer.Analyze(prodCompilation, testCompilation);

        Assert.HasCount(0, result.ZombieIslands, "Completely unreferenced method should not be a ZombieIsland");
        Assert.HasCount(1, result.IsolatedDeadMethods, "Completely unreferenced method should be in IsolatedDeadMethods");
    }

    [TestMethod]
    public void Analyze_ExtensionMethodInvocation_ResolvesDeclaringMethodAsLive() {
        string prodSource = @"
namespace PlayMore35;

public class Target {
}

public static class TargetExtensions {
    public static Target Initialize(this Target target) {
        DownstreamWork();
        return target;
    }

    private static void DownstreamWork() {}
}

public class Program {
    public static void Main() {
        var t = new Target().Initialize();
    }
}
";
        var prodCompilation = TestCompilationHelper.CreateCompilation("PlayMore35", prodSource);
        var testCompilation = TestCompilationHelper.CreateCompilation("PlayMore35.Tests", "// empty");

        var analyzer = new ReachabilityAnalyzer();
        var result = analyzer.Analyze(prodCompilation, testCompilation);

        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "Initialize"), "TargetExtensions.Initialize should be Live");
        Assert.IsTrue(result.LiveMethods.Any(m => m.Name == "DownstreamWork"), "DownstreamWork should be Live");
        Assert.IsFalse(result.DeadMethods.Any(m => m.Name == "Initialize"), "Initialize should not be Dead");
        Assert.IsFalse(result.DeadMethods.Any(m => m.Name == "DownstreamWork"), "DownstreamWork should not be Dead");
    }
}
