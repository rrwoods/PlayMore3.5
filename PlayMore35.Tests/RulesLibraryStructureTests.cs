using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlayMore35.Tests
{
    [TestClass]
    public class RulesLibraryStructureTests
    {
        public const int MaxLinesPerRulesFile = 300;

        private static string FindRepositoryRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PlayMore35.sln")))
            {
                dir = dir.Parent;
            }

            Assert.IsNotNull(dir, "Could not locate repository root containing PlayMore35.sln.");
            return dir.FullName;
        }

        [TestMethod]
        public void RulesLibraryFiles_DoNotExceedMaxLineThreshold()
        {
            string repoRoot = FindRepositoryRoot();
            string rulesDir = Path.Combine(repoRoot, "docs", "rules");
            Assert.IsTrue(Directory.Exists(rulesDir), $"Expected rules library directory at '{rulesDir}'.");

            string[] markdownFiles = Directory.GetFiles(rulesDir, "*.md", SearchOption.AllDirectories);
            Assert.IsTrue(markdownFiles.Length > 0, "Expected at least one markdown file under docs/rules/.");

            var oversizedFiles = new List<string>();
            foreach (string filePath in markdownFiles)
            {
                int lineCount = File.ReadLines(filePath).Count();
                if (lineCount > MaxLinesPerRulesFile)
                {
                    string relativePath = filePath.Substring(repoRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    oversizedFiles.Add($"{relativePath} ({lineCount} lines > {MaxLinesPerRulesFile})");
                }
            }

            Assert.AreEqual(
                0,
                oversizedFiles.Count,
                "The following docs/rules/ files exceed the 300-line progressive disclosure limit. " +
                "Run a /grilling session with the maintainer to choose subgroupings and split <stem>.md into <stem>/: " +
                string.Join(", ", oversizedFiles));
        }

        [TestMethod]
        public void ContentResourceJsonFiles_HaveMatchingCitationFileOrDirectory()
        {
            string repoRoot = FindRepositoryRoot();
            string resourcesDir = Path.Combine(repoRoot, "PlayMore35", "Resources");
            string contentRulesDir = Path.Combine(repoRoot, "docs", "rules", "content");

            Assert.IsTrue(Directory.Exists(resourcesDir), $"Expected Resources directory at '{resourcesDir}'.");
            Assert.IsTrue(Directory.Exists(contentRulesDir), $"Expected content rules directory at '{contentRulesDir}'.");

            string[] jsonFiles = Directory.GetFiles(resourcesDir, "*.json", SearchOption.TopDirectoryOnly);
            var missingTargets = new List<string>();

            foreach (string jsonPath in jsonFiles)
            {
                string stem = Path.GetFileNameWithoutExtension(jsonPath);
                if (string.Equals(stem, "rules", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string singleFileTarget = Path.Combine(contentRulesDir, stem + ".md");
                string splitDirTarget = Path.Combine(contentRulesDir, stem);

                bool hasSingleFile = File.Exists(singleFileTarget);
                bool hasSplitDir = Directory.Exists(splitDirTarget) &&
                                   Directory.GetFiles(splitDirTarget, "*.md", SearchOption.AllDirectories).Length > 0;

                if (!hasSingleFile && !hasSplitDir)
                {
                    missingTargets.Add($"Resources/{stem}.json -> expected docs/rules/content/{stem}.md or docs/rules/content/{stem}/*.md");
                }
            }

            Assert.AreEqual(
                0,
                missingTargets.Count,
                "Every content JSON file in PlayMore35/Resources/ must have a matching citation file or split directory in docs/rules/content/: " +
                string.Join("; ", missingTargets));
        }
    }
}
