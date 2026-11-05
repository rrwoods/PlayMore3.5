using System;
using System.IO;
using System.Linq;

namespace DeadCodeScanner;

class Program {
    static int Main(string[] args) {
        bool isCheckMode = args.Contains("--check");
        string outputPath = "reports/dead-code-report.md";

        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == "--output" || args[i] == "-o") {
                outputPath = args[i + 1];
            }
        }

        string repoRoot = SolutionScanner.FindRepoRoot(Directory.GetCurrentDirectory());
        Console.WriteLine($"[DeadCodeScanner] Scanning solution at: {repoRoot}");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = SolutionScanner.Scan(repoRoot);
        stopwatch.Stop();

        Console.WriteLine($"[DeadCodeScanner] Scan completed in {stopwatch.ElapsedMilliseconds} ms.");
        Console.WriteLine($"  Total methods: {result.LiveMethods.Count + result.DeadMethods.Count}");
        Console.WriteLine($"  Live methods:  {result.LiveMethods.Count}");
        Console.WriteLine($"  Dead methods:  {result.DeadMethods.Count}");
        Console.WriteLine($"  Zombie Islands: {result.ZombieIslands.Count}");

        var formatter = new MarkdownReportFormatter();
        string reportMarkdown = formatter.Format(result);

        string fullOutputPath = Path.IsPathRooted(outputPath)
            ? outputPath
            : Path.Combine(repoRoot, outputPath);

        string? dir = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullOutputPath, reportMarkdown);
        Console.WriteLine($"[DeadCodeScanner] Full report written to: {fullOutputPath}");

        if (isCheckMode && result.ZombieIslands.Count > 0) {
            Console.Error.WriteLine($"[DeadCodeScanner] ERROR: Found {result.ZombieIslands.Count} zombie islands with {result.DeadMethods.Count} dead methods.");
            return 1;
        }

        return 0;
    }
}
