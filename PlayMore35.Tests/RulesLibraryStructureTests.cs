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

        [TestMethod]
        public void RulesLibraryFiles_DoNotExceedMaxLineThreshold()
        {
            string repoRoot = TestEnvironment.FindRepoRoot();
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
            string repoRoot = TestEnvironment.FindRepoRoot();
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

        [TestMethod]
        public void RulesLibraryFiles_HaveNoStraySoftHyphensOrCorruptedLigatures()
        {
            string repoRoot = TestEnvironment.FindRepoRoot();
            string rulesDir = Path.Combine(repoRoot, "docs", "rules");
            Assert.IsTrue(Directory.Exists(rulesDir), $"Expected rules library directory at '{rulesDir}'.");

            string[] markdownFiles = Directory.GetFiles(rulesDir, "*.md", SearchOption.AllDirectories);
            Assert.IsTrue(markdownFiles.Length > 0, "Expected at least one markdown file under docs/rules/.");

            // Match stray soft-hyphen tokens split by whitespace (e.g. "war- blade", "stan- dard", "crea- tures")
            var softHyphenRegex = new System.Text.RegularExpressions.Regex(@"\b[A-Za-z]+-\s+[A-Za-z]+\b", System.Text.RegularExpressions.RegexOptions.Compiled);

            // Raw typographic ligatures or unicode replacement characters
            char[] corruptedChars = new char[] { '\uFB00', '\uFB01', '\uFB02', '\uFB03', '\uFB04', '\uFFFD' };

            var violations = new List<string>();

            foreach (string filePath in markdownFiles)
            {
                string relativePath = filePath.Substring(repoRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string content = File.ReadAllText(filePath);

                var hyphenMatches = softHyphenRegex.Matches(content);
                foreach (System.Text.RegularExpressions.Match m in hyphenMatches)
                {
                    violations.Add($"{relativePath}: stray soft hyphen '{m.Value}'");
                }

                foreach (char c in corruptedChars)
                {
                    if (content.IndexOf(c) >= 0)
                    {
                        violations.Add($"{relativePath}: corrupted ligature or replacement char '\\u{(int)c:X4}'");
                    }
                }
            }

            Assert.AreEqual(
                0,
                violations.Count,
                "Markdown files in docs/rules/ must not contain unresolved soft hyphens or raw typographic ligatures:\n" +
                string.Join("\n", violations));
        }
    }
}
