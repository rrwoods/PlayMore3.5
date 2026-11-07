using System;
using System.Linq;
using System.Text;

namespace DeadCodeScanner;

public class MarkdownReportFormatter {
    public string Format(AnalysisResult result) {
        var sb = new StringBuilder();

        sb.AppendLine("# Dead Code & Zombie Island Report");
        sb.AppendLine();
        sb.AppendLine($"*Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");
        sb.AppendLine();

        int totalMethods = result.LiveMethods.Count + result.DeadMethods.Count;
        double livePct = totalMethods > 0 ? (100.0 * result.LiveMethods.Count / totalMethods) : 100.0;

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("| :--- | :--- |");
        sb.AppendLine($"| **Total Methods Scanned** | {totalMethods} |");
        sb.AppendLine($"| **Live Methods (Reachable from UI/Main)** | {result.LiveMethods.Count} ({livePct:F1}%) |");
        sb.AppendLine($"| **Dead Methods (Unreachable)** | {result.DeadMethods.Count} |");
        sb.AppendLine($"| **Zombie Islands Detected** | {result.ZombieIslands.Count} |");
        sb.AppendLine($"| **Isolated Dead Methods** | {result.IsolatedDeadMethods.Count} |");
        sb.AppendLine();

        if (result.ZombieIslands.Count == 0) {
            sb.AppendLine("> [!NOTE]");
            sb.AppendLine("> No zombie islands detected! All code paths are either reachable from application roots or completely isolated.");
            return sb.ToString();
        }

        sb.AppendLine("## Zombie Islands Breakdown");
        sb.AppendLine();
        sb.AppendLine("A **Zombie Island** is an unreachable call tree that appears alive because it has callers or is kept on artificial life support by unit tests.");
        sb.AppendLine();

        foreach (var island in result.ZombieIslands) {
            sb.AppendLine($"### `{island.Name}`");
            sb.AppendLine();

            sb.AppendLine("**Island Roots (No production callers):**");
            foreach (var root in island.Roots) {
                sb.AppendLine($"- `{root.ContainingType}.{root.Name}` ({root.FilePath}:{root.LineNumber})");
            }
            sb.AppendLine();

            if (island.Cascade.Count > 0) {
                sb.AppendLine("**Cascaded Methods (Downstream dead calls):**");
                foreach (var cascaded in island.Cascade) {
                    sb.AppendLine($"- `{cascaded.ContainingType}.{cascaded.Name}` ({cascaded.FilePath}:{cascaded.LineNumber})");
                }
                sb.AppendLine();
            }

            sb.AppendLine("**Artificial Life Support (Tests referencing this island):**");
            if (island.LifeSupportTests.Count > 0) {
                foreach (var test in island.LifeSupportTests) {
                    sb.AppendLine($"- 🧪 `{test.ContainingType}.{test.Name}` ({test.FilePath}:{test.LineNumber})");
                }
            } else {
                sb.AppendLine("- *None (Pure dead code - no tests reference this island)*");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        if (result.IsolatedDeadMethods.Count > 0) {
            sb.AppendLine("## Isolated Dead Methods");
            sb.AppendLine();
            sb.AppendLine("These methods have zero callers in production and zero tests referencing them (completely standalone dead code):");
            sb.AppendLine();
            foreach (var isolated in result.IsolatedDeadMethods) {
                sb.AppendLine($"- `{isolated.ContainingType}.{isolated.Name}` ({isolated.FilePath}:{isolated.LineNumber})");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
