using System;
using System.IO;

namespace PlayMore35.Tests {
    public static class TestEnvironment {
        private static readonly Lazy<string> repoRoot = new Lazy<string>(() => {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PlayMore35.sln"))) {
                dir = dir.Parent;
            }
            if (dir == null) {
                throw new DirectoryNotFoundException("Could not locate repository root containing PlayMore35.sln.");
            }
            return dir.FullName;
        });

        public static string FindRepoRoot() => repoRoot.Value;

        public static string FindOdricJsonPath() => Path.Combine(FindRepoRoot(), "Odric.json");
    }
}
