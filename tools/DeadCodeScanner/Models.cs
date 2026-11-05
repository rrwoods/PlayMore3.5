using System.Collections.Generic;

namespace DeadCodeScanner;

public record MethodNode(
    string Id,
    string Name,
    string ContainingType,
    string FilePath,
    int LineNumber,
    bool IsRoot
);

public record ZombieIsland(
    string Name,
    IReadOnlyList<MethodNode> Roots,
    IReadOnlyList<MethodNode> Cascade,
    IReadOnlyList<MethodNode> LifeSupportTests
);

public record AnalysisResult(
    IReadOnlyList<MethodNode> LiveMethods,
    IReadOnlyList<MethodNode> DeadMethods,
    IReadOnlyList<ZombieIsland> ZombieIslands
);
