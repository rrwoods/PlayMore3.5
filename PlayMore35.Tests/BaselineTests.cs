using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.UI;

namespace PlayMore35.Tests {
    [TestClass]
    public class BaselineTests {
        [TestMethod]
        public void Test_Runner_Executes_Headlessly() {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void PlayMore35Csproj_WinFormsTypesHaveExpectedSubType() {
            var csprojPath = Path.Combine(TestEnvironment.FindRepoRoot(), "PlayMore35", "PlayMore35.csproj");
            var doc = new XmlDocument();
            doc.Load(csprojPath);

            var subTypesByClassName = new Dictionary<string, string>(StringComparer.Ordinal);
            var compileNodes = doc.GetElementsByTagName("Compile");
            foreach (XmlElement compile in compileNodes) {
                var include = compile.GetAttribute("Include");
                if (string.IsNullOrEmpty(include) || include.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var className = Path.GetFileNameWithoutExtension(include);
                var subTypeNodes = compile.GetElementsByTagName("SubType");
                var subType = subTypeNodes.Count > 0 ? subTypeNodes[0].InnerText : null;
                subTypesByClassName[className] = subType;
            }

            var componentTypes = typeof(MainForm).Assembly
                .GetTypes()
                .Where(t => !t.IsNested && typeof(Component).IsAssignableFrom(t))
                .OrderBy(t => t.Name);

            var mismatches = new List<string>();
            foreach (var type in componentTypes) {
                if (!subTypesByClassName.TryGetValue(type.Name, out var actualSubType)) {
                    continue;
                }

                var expectedSubType = typeof(Form).IsAssignableFrom(type)
                    ? "Form"
                    : typeof(UserControl).IsAssignableFrom(type)
                        ? "UserControl"
                        : "Component";

                if (actualSubType != expectedSubType) {
                    mismatches.Add($"{type.Name}.cs: expected <SubType>{expectedSubType}</SubType>, found '{actualSubType ?? "(none)"}'");
                }
            }

            Assert.AreEqual(
                0,
                mismatches.Count,
                "Visual Studio requires matching <SubType> metadata in PlayMore35.csproj:\n" + string.Join("\n", mismatches)
            );
        }

        [TestMethod]
        public void PlayMore35Csproj_TopLevelItemGroupsHaveNoDuplicateFiles() {
            var projectDir = Path.Combine(TestEnvironment.FindRepoRoot(), "PlayMore35");
            var csprojPath = Path.Combine(projectDir, "PlayMore35.csproj");
            var doc = new XmlDocument();
            doc.Load(csprojPath);

            var itemTypesByPath = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode child in doc.DocumentElement.ChildNodes) {
                if (!(child is XmlElement itemGroup) || itemGroup.Name != "ItemGroup") {
                    continue;
                }

                foreach (XmlNode itemNode in itemGroup.ChildNodes) {
                    if (!(itemNode is XmlElement item) || item.Name == "Reference" || item.Name == "ProjectReference") {
                        continue;
                    }

                    var include = item.GetAttribute("Include");
                    if (string.IsNullOrEmpty(include)) {
                        continue;
                    }

                    IEnumerable<string> resolvedPaths;
                    if (include.IndexOfAny(new[] { '*', '?' }) >= 0) {
                        var dirPart = Path.GetDirectoryName(include) ?? string.Empty;
                        var pattern = Path.GetFileName(include);
                        var searchDir = Path.Combine(projectDir, dirPart);
                        resolvedPaths = Directory.Exists(searchDir)
                            ? Directory.GetFiles(searchDir, pattern).Select(f => Path.Combine(dirPart, Path.GetFileName(f)))
                            : Enumerable.Empty<string>();
                    } else {
                        resolvedPaths = new[] { include };
                    }

                    foreach (var relativePath in resolvedPaths) {
                        var normalized = relativePath.Replace('/', '\\');
                        if (!itemTypesByPath.TryGetValue(normalized, out var itemTypes)) {
                            itemTypes = new List<string>();
                            itemTypesByPath[normalized] = itemTypes;
                        }
                        itemTypes.Add(item.Name);
                    }
                }
            }

            var duplicates = itemTypesByPath
                .Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => $"{kvp.Key} ({string.Join(", ", kvp.Value)})")
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.AreEqual(
                0,
                duplicates.Count,
                "Duplicate top-level ItemGroup entries cause duplicate nodes in Solution Explorer:\n" + string.Join("\n", duplicates)
            );
        }
    }
}
