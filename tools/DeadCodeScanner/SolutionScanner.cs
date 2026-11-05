using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DeadCodeScanner;

public static class SolutionScanner {
    public static string FindRepoRoot(string startDir) {
        var current = new DirectoryInfo(startDir);
        while (current != null) {
            if (File.Exists(Path.Combine(current.FullName, "PlayMore35.sln"))) {
                return current.FullName;
            }
            current = current.Parent;
        }
        return startDir;
    }

    public static AnalysisResult Scan(string repoRoot) {
        string prodDir = Path.Combine(repoRoot, "PlayMore35");
        string testDir = Path.Combine(repoRoot, "PlayMore35.Tests");

        if (!Directory.Exists(prodDir)) {
            throw new DirectoryNotFoundException($"Could not find PlayMore35 directory at {prodDir}");
        }

        var prodFiles = GetSourceFiles(prodDir);
        var testFiles = Directory.Exists(testDir) ? GetSourceFiles(testDir) : new List<string>();

        var references = ResolveMetadataReferences(repoRoot);

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        var prodTrees = prodFiles
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, f))
            .ToList();

        var prodCompilation = CSharpCompilation.Create(
            "PlayMore35",
            prodTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var allReferencesForTests = new List<MetadataReference>(references) {
            prodCompilation.ToMetadataReference()
        };

        var testTrees = testFiles
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, f))
            .ToList();

        var testCompilation = CSharpCompilation.Create(
            "PlayMore35.Tests",
            testTrees,
            allReferencesForTests,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var analyzer = new ReachabilityAnalyzer();
        return analyzer.Analyze(prodCompilation, testCompilation);
    }

    private static List<string> GetSourceFiles(string directory) {
        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                           !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .ToList();
    }

    private static List<MetadataReference> ResolveMetadataReferences(string repoRoot) {
        var references = new List<MetadataReference>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddRef(string path) {
            if (File.Exists(path) && addedPaths.Add(path)) {
                try {
                    references.Add(MetadataReference.CreateFromFile(path));
                } catch {
                    // Ignore unloadable assemblies
                }
            }
        }

        // 1. .NET Framework standard reference directory on Windows
        string netFrameworkDir = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319";
        if (!Directory.Exists(netFrameworkDir)) {
            netFrameworkDir = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319";
        }

        if (Directory.Exists(netFrameworkDir)) {
            string[] coreDlls = { "mscorlib.dll", "System.dll", "System.Core.dll", "System.Windows.Forms.dll", "System.Drawing.dll", "System.Xml.dll", "System.Data.dll" };
            foreach (var dll in coreDlls) {
                AddRef(Path.Combine(netFrameworkDir, dll));
            }
        }

        // 2. PlayMore35\bin\Debug\ DLLs (Newtonsoft.Json, System.Memory, etc.)
        string binDebug = Path.Combine(repoRoot, "PlayMore35", "bin", "Debug");
        if (Directory.Exists(binDebug)) {
            foreach (var dll in Directory.GetFiles(binDebug, "*.dll")) {
                AddRef(dll);
            }
        }

        // 3. Fallback to current runtime assemblies if framework not found
        if (references.Count == 0) {
            AddRef(typeof(object).Assembly.Location);
            AddRef(typeof(Console).Assembly.Location);
        }

        return references;
    }
}
