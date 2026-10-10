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
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PlayMore35", "PlayMore35.csproj"))) {
                dir = dir.Parent;
            }
            Assert.IsNotNull(dir, "Could not locate PlayMore35/PlayMore35.csproj from test base directory.");

            var csprojPath = Path.Combine(dir.FullName, "PlayMore35", "PlayMore35.csproj");
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
    }
}
