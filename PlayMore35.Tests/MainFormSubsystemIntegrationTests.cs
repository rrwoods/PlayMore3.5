using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;
using PlayMore35.UI.Subsystems.PactMagic;

namespace PlayMore35.Tests {
    [TestClass]
    public class MainFormSubsystemIntegrationTests {
        [TestMethod]
        public void MainForm_Implements_ISubsystemUIHost() {
            Assert.IsTrue(typeof(ISubsystemUIHost).IsAssignableFrom(typeof(MainForm)),
                "MainForm must explicitly implement ISubsystemUIHost.");
        }

        [TestMethod]
        public void MainForm_HasZeroLegacyPactMagicControlsOrFields() {
            var fields = typeof(MainForm).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            string[] forbiddenFieldNames = {
                "pagVestiges", "pnlVestige1", "pnlVestige2", "optVestige1", "optVestige2",
                "lblPactState", "lstPactState", "vestigePanels", "stateLabels", "pactStates",
                "vestigeChoices", "vestigePanelBuffer", "vestigeChoiceBuffer"
            };

            foreach (var name in forbiddenFieldNames) {
                Assert.IsFalse(fields.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)),
                    $"MainForm must not contain field '{name}'.");
            }

            var methods = typeof(MainForm).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            string[] forbiddenMethodNames = {
                "VestigesTab_Enter", "ChooseVestige", "BindVestige", "ChangePactState"
            };

            foreach (var name in forbiddenMethodNames) {
                Assert.IsFalse(methods.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)),
                    $"MainForm must not contain method '{name}'.");
            }
        }

        [TestMethod]
        public void MainForm_ZeroStaticPactMagicLiteracyLeaks() {
            string mainFormPath = Path.Combine(TestEnvironment.FindRepoRoot(), "PlayMore35", "UI", "MainForm.cs");
            string designerPath = Path.Combine(TestEnvironment.FindRepoRoot(), "PlayMore35", "UI", "MainForm.Designer.cs");

            string[] forbiddenTokens = {
                "pagVestiges", "pnlVestige1", "pnlVestige2", "optVestige1", "optVestige2",
                "lblPactState", "lstPactState", "vestigePanels", "stateLabels", "pactStates",
                "vestigeChoices", "VestigesTab_Enter", "ChooseVestige", "BindVestige", "ChangePactState"
            };

            string mainFormCode = File.ReadAllText(mainFormPath);
            string designerCode = File.ReadAllText(designerPath);

            foreach (var token in forbiddenTokens) {
                Assert.IsFalse(mainFormCode.Contains(token), $"MainForm.cs must not contain token '{token}'.");
                Assert.IsFalse(designerCode.Contains(token), $"MainForm.Designer.cs must not contain token '{token}'.");
            }
        }

        [TestMethod]
        public void MainForm_NonBinderCharacter_DoesNotInjectPactMagicTab() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "human" };
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                var tabMain = GetTabControl(form);
                Assert.IsNotNull(tabMain);

                // Subsystem tabs should not contain any PactMagicTabPage
                var pactTab = tabMain.TabPages.OfType<PactMagicTabPage>().FirstOrDefault();
                Assert.IsNull(pactTab, "Non-binder character must not have PactMagicTabPage injected.");

                // Tab names should not contain "Vestiges"
                var vestigeTab = tabMain.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "Vestiges");
                Assert.IsNull(vestigeTab, "Non-binder character must not have a Vestiges tab.");
            }
        }

        [TestMethod]
        public void MainForm_BinderCharacter_DynamicallyInjectsPactMagicTabPage() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            using (var form = new MainForm(session)) {
                form.Show();
                Application.DoEvents();

                var tabMain = GetTabControl(form);
                Assert.IsNotNull(tabMain);

                var pactTab = tabMain.TabPages.OfType<PactMagicTabPage>().FirstOrDefault();
                Assert.IsNotNull(pactTab, "Binder character must have PactMagicTabPage injected.");
                Assert.AreEqual("Vestiges", pactTab.Text);

                // Verify insertion at index 1
                Assert.AreEqual(1, tabMain.TabPages.IndexOf(pactTab), "PactMagicTabPage should be at SUBSYSTEMS_INDEX (1).");
            }
        }

        [TestMethod]
        public void MainForm_SubsystemTabLifecycle_AddsWhenGained_RemovesWhenLost() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "human" };
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                form.Show();
                Application.DoEvents();

                var tabMain = GetTabControl(form);
                Assert.IsNull(tabMain.TabPages.OfType<PactMagicTabPage>().FirstOrDefault());

                // Level up to Binder
                session.Mutate(c => c.classLevels[0] = "Binder");
                session.Recalculate();
                form.RefreshEffects();
                Application.DoEvents();

                var pactTab = tabMain.TabPages.OfType<PactMagicTabPage>().FirstOrDefault();
                Assert.IsNotNull(pactTab, "Tab should be injected after gaining Binder level.");
                Assert.AreEqual(1, tabMain.TabPages.IndexOf(pactTab));

                // Change class away from Binder
                session.Mutate(c => c.classLevels[0] = "Fighter");
                session.Recalculate();
                form.RefreshEffects();
                Application.DoEvents();

                Assert.IsNull(tabMain.TabPages.OfType<PactMagicTabPage>().FirstOrDefault(),
                    "Tab should be removed after losing Binder level.");
            }
        }

        [TestMethod]
        public void MainForm_HostSeam_DelegatesMethodsCorrectly() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                form.Show();
                Application.DoEvents();

                var host = (ISubsystemUIHost)form;
                Assert.AreSame(session, host.Session);

                // Ensure methods can be invoked without exception
                host.RequestRecalculate();
                host.RequestEffectsRefresh();
                host.RequestTimelineRefresh();
            }
        }

        [TestMethod]
        public void MainForm_LoadsInitialCharacterPath_WhenSupplied() {
            string odricPath = TestEnvironment.FindOdricJsonPath();
            using (var form = new MainForm(initialCharacterPath: odricPath)) {
                form.Show();
                Application.DoEvents();

                Assert.IsNotNull(form.Session);
                Assert.AreEqual("Odric Greatshield", form.Session.Character.name);
            }
        }

        private static TabControl GetTabControl(MainForm form) {
            var field = typeof(MainForm).GetField("tabMain", BindingFlags.NonPublic | BindingFlags.Instance);
            return (TabControl)field.GetValue(form);
        }
    }
}
