using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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

        [TestMethod]
        public void MigratedContentFiles_HaveNoChecklistSyntax()
        {
            string repoRoot = TestEnvironment.FindRepoRoot();
            string contentRulesDir = Path.Combine(repoRoot, "docs", "rules", "content");
            string classesDir = Path.Combine(contentRulesDir, "classes");
            string featsDir = Path.Combine(contentRulesDir, "feats");

            var directoriesToCheck = new List<string>();
            if (Directory.Exists(classesDir)) directoriesToCheck.Add(classesDir);
            if (Directory.Exists(featsDir)) directoriesToCheck.Add(featsDir);

            var checklistRegex = new Regex(@"^\s*-\s*\[[ xX]\]", RegexOptions.Multiline);
            var violations = new List<string>();

            foreach (string dir in directoriesToCheck)
            {
                foreach (string file in Directory.GetFiles(dir, "*.md", SearchOption.AllDirectories))
                {
                    string text = File.ReadAllText(file);
                    if (checklistRegex.IsMatch(text))
                    {
                        string relativePath = file.Substring(repoRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        violations.Add(relativePath);
                    }
                }
            }

            Assert.AreEqual(
                0,
                violations.Count,
                "Content files must use structured reference schemas instead of task-list checklist syntax (- [ ] or - [x]):\n" +
                string.Join("\n", violations));
        }

        [TestMethod]
        public void FeatsDirectory_HasBalancedFilesCoveringAllResourceFeats()
        {
            string repoRoot = TestEnvironment.FindRepoRoot();
            string contentRulesDir = Path.Combine(repoRoot, "docs", "rules", "content");
            string singleFeatsFile = Path.Combine(contentRulesDir, "feats.md");
            string featsDir = Path.Combine(contentRulesDir, "feats");

            Assert.IsFalse(File.Exists(singleFeatsFile), "docs/rules/content/feats.md should be replaced by docs/rules/content/feats/ directory.");
            Assert.IsTrue(Directory.Exists(featsDir), "Expected directory docs/rules/content/feats/ to exist.");

            string featsAI = Path.Combine(featsDir, "feats-a-i.md");
            string featsIZ = Path.Combine(featsDir, "feats-i-z.md");

            Assert.IsTrue(File.Exists(featsAI), "Expected docs/rules/content/feats/feats-a-i.md to exist.");
            Assert.IsTrue(File.Exists(featsIZ), "Expected docs/rules/content/feats/feats-i-z.md to exist.");

            int linesAI = File.ReadLines(featsAI).Count();
            int linesIZ = File.ReadLines(featsIZ).Count();
            Assert.IsTrue(linesAI <= MaxLinesPerRulesFile, $"feats-a-i.md ({linesAI} lines) exceeds {MaxLinesPerRulesFile} lines.");
            Assert.IsTrue(linesIZ <= MaxLinesPerRulesFile, $"feats-i-z.md ({linesIZ} lines) exceeds {MaxLinesPerRulesFile} lines.");

            var featsInAI = ExtractSectionHeadings(featsAI);
            var featsInIZ = ExtractSectionHeadings(featsIZ);

            Assert.IsTrue(Math.Abs(featsInAI.Count - featsInIZ.Count) <= 2,
                $"Feat files should have balanced entry counts. Got {featsInAI.Count} in feats-a-i.md and {featsInIZ.Count} in feats-i-z.md.");

            string featsJsonPath = Path.Combine(repoRoot, "PlayMore35", "Resources", "feats.json");
            string jsonText = File.ReadAllText(featsJsonPath);
            var parsedFeats = JArray.Parse(jsonText);
            var allDocumentedFeats = new HashSet<string>(featsInAI.Concat(featsInIZ), StringComparer.OrdinalIgnoreCase);
            var missingFeats = new List<string>();

            foreach (var item in parsedFeats)
            {
                string featName = (string)item["benefit"]?["name"];
                if (!string.IsNullOrEmpty(featName) && !allDocumentedFeats.Contains(featName))
                {
                    missingFeats.Add(featName);
                }
            }

            Assert.AreEqual(0, missingFeats.Count, "The following feats from feats.json are missing from docs/rules/content/feats/: " + string.Join(", ", missingFeats));
        }

        private static List<string> ExtractSectionHeadings(string filePath)
        {
            var headingRegex = new Regex(@"^###\s+(.+)$", RegexOptions.Multiline);
            return headingRegex.Matches(File.ReadAllText(filePath))
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim())
                .ToList();
        }
    }
}
