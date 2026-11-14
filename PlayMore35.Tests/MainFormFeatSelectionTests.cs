using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.UI;

namespace PlayMore35.Tests {
    [TestClass]
    public class MainFormFeatSelectionTests {
        private const uint SEM_FAILCRITICALERRORS = 0x0001;
        private const uint SEM_NOGPFAULTERRORBOX = 0x0002;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_VSCROLL = 0x0115;
        private const int SB_LINEDOWN = 1;
        private const int MK_LBUTTON = 0x0001;
        private const int CB_GETCOMBOBOXINFO = 0x0164;
        private const int LB_SETTOPINDEX = 0x0197;
        private const int LB_GETITEMRECT = 0x0198;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        [DllImport("kernel32.dll")]
        private static extern uint SetErrorMode(uint uMode);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, ref COMBOBOXINFO lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr MakeLParam(int loWord, int hiWord) {
            return (IntPtr)((hiWord << 16) | (loWord & 0xFFFF));
        }

        [TestMethod]
        public void SelectFeat_UnselectedSlotDropdownClick_DoesNotDestroyComboBoxHandleWhileComboBoxWndProcOnStack() {
            bool destroyedOnComboStack = false;
            string? capturedFeat = null;
            string? comboText = null;
            FontStyle finalStyle = FontStyle.Italic;

            Exception? caught = RunOnStaThread(() => {
                using var form = new MainForm(initialCharacterPath: null);
                var pagCharacter = GetField<TabPage>(form, "pagCharacter");

                int step = 0;
                ComboBox? featCombo = null;
                IntPtr listHwnd = IntPtr.Zero;

                using var timer = new System.Windows.Forms.Timer { Interval = 20 };
                timer.Tick += (s, e) => {
                    timer.Stop();
                    step++;
                    if (step == 1) {
                        featCombo = pagCharacter.Controls.OfType<ComboBox>()
                            .First(c => c.Visible && c.AutoCompleteMode != AutoCompleteMode.None);
                        Assert.AreEqual(FontStyle.Italic, featCombo.Font.Style, "Unselected feat slot should start in Italic style.");

                        featCombo.HandleDestroyed += (hs, he) => {
                            if (form.Disposing || form.IsDisposed || step >= 3) return;
                            string stack = new StackTrace(false).ToString();
                            if (IsReEntrantComboBoxDestruction(stack)) {
                                destroyedOnComboStack = true;
                            }
                        };

                        featCombo.Focus();
                        featCombo.DroppedDown = true;
                        var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO)) };
                        SendMessage(featCombo.Handle, CB_GETCOMBOBOXINFO, 0, ref info);
                        listHwnd = info.hwndList;
                    } else if (step == 2) {
                        int targetIdx = FindComboIndex(featCombo!, "Improved Binding");
                        SendMessage(listHwnd, LB_SETTOPINDEX, (IntPtr)targetIdx, IntPtr.Zero);
                        ClickDropdownItem(listHwnd, targetIdx);
                    } else {
                        var slot = (ProgressionSlotKey)featCombo!.Tag;
                        capturedFeat = form.Session.GetFeat(slot);
                        comboText = featCombo.Text;
                        finalStyle = featCombo.Font.Style;
                        Application.ExitThread();
                        return;
                    }
                    timer.Start();
                };

                form.Shown += (s, e) => timer.Start();
                Application.Run(form);
            });

            if (caught != null) {
                Assert.Fail($"Unexpected exception during feat selection: {caught}");
            }
            Assert.IsFalse(destroyedOnComboStack,
                "ComboBox native HWND must not be destroyed while ComboBox.WndProc or ComboBoxChildNativeWindow.WndProc is on the call stack (causes native AccessViolationException in comctl32.dll).");
            Assert.AreEqual("Improved Binding", capturedFeat);
            Assert.AreEqual("Improved Binding", comboText);
            Assert.AreEqual(FontStyle.Regular, finalStyle);
        }

        [TestMethod]
        public void SelectFeat_CharacterCreationFlowWithDropdownScrollAndClick_SelectsImprovedBindingCleanly() {
            bool destroyedOnComboStack = false;
            string? capturedFeat = null;
            string? comboText = null;
            FontStyle finalStyle = FontStyle.Italic;
            bool finalVisible = false;

            Exception? caught = RunOnStaThread(() => {
                using var form = new MainForm(initialCharacterPath: null);
                var pagCharacter = GetField<TabPage>(form, "pagCharacter");

                int step = 0;
                ComboBox? featCombo = null;
                IntPtr listHwnd = IntPtr.Zero;
                int targetIdx = -1;

                using var timer = new System.Windows.Forms.Timer { Interval = 20 };
                timer.Tick += (s, e) => {
                    timer.Stop();
                    step++;
                    if (step == 1) {
                        GetField<TextBox>(form, "txtName").Text = "Odric Greatshield";
                        GetField<TextBox>(form, "txtAlignment").Text = "NG";
                        GetField<TextBox>(form, "txtGender").Text = "Male";

                        var cboRace = GetField<ComboBox>(form, "cboRace");
                        cboRace.SelectedIndex = FindComboIndex(cboRace, "dream dwarf");

                        string[] fields = { "txtAssignedStr", "txtAssignedDex", "txtAssignedCon", "txtAssignedInt", "txtAssignedWis", "txtAssignedCha" };
                        string[] vals = { "14", "8", "17", "14", "13", "10" };
                        for (int i = 0; i < fields.Length; i++) {
                            var tb = GetField<TextBox>(form, fields[i]);
                            tb.Focus();
                            tb.Text = vals[i];
                            Application.DoEvents();
                        }

                        var classCombo = pagCharacter.Controls.OfType<ComboBox>()
                            .First(c => c.Visible && c.Items.Cast<object>().Any(it => it?.ToString() == "Binder"));
                        classCombo.Focus();
                        classCombo.SelectedIndex = FindComboIndex(classCombo, "Binder");
                    } else if (step == 2) {
                        featCombo = pagCharacter.Controls.OfType<ComboBox>()
                            .First(c => c.Visible && c.Items.Cast<object>().Any(it => it?.ToString() == "Improved Binding"));
                        targetIdx = FindComboIndex(featCombo, "Improved Binding");

                        featCombo.HandleDestroyed += (hs, he) => {
                            if (form.Disposing || form.IsDisposed || step >= 4) return;
                            string stack = new StackTrace(false).ToString();
                            if (IsReEntrantComboBoxDestruction(stack)) {
                                destroyedOnComboStack = true;
                            }
                        };

                        featCombo.Focus();
                        featCombo.DroppedDown = true;
                        var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO)) };
                        SendMessage(featCombo.Handle, CB_GETCOMBOBOXINFO, 0, ref info);
                        listHwnd = info.hwndList;

                        for (int w = 0; w < 40; w++) {
                            SendMessage(listHwnd, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
                        }
                    } else if (step == 3) {
                        ClickDropdownItem(listHwnd, targetIdx);
                    } else {
                        var slot = (ProgressionSlotKey)featCombo!.Tag;
                        capturedFeat = form.Session.GetFeat(slot);
                        comboText = featCombo.Text;
                        finalStyle = featCombo.Font.Style;
                        finalVisible = featCombo.Visible && featCombo.IsHandleCreated;
                        Application.ExitThread();
                        return;
                    }
                    timer.Start();
                };

                form.Shown += (s, e) => timer.Start();
                Application.Run(form);
            });

            if (caught != null) {
                Assert.Fail($"Unexpected exception during character creation feat selection: {caught}");
            }
            Assert.IsFalse(destroyedOnComboStack,
                "ComboBox native HWND must not be destroyed while ComboBox.WndProc or ComboBoxChildNativeWindow.WndProc is on the call stack.");
            Assert.AreEqual("Improved Binding", capturedFeat);
            Assert.AreEqual("Improved Binding", comboText);
            Assert.AreEqual(FontStyle.Regular, finalStyle);
            Assert.IsTrue(finalVisible);
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private static Exception? RunOnStaThread(Action body) {
            SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);
            Exception? caught = null;

            var staThread = new Thread(() => {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                try {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    body();
                } catch (Exception ex) {
                    caught = ex;
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();
            Assert.IsTrue(staThread.Join(TimeSpan.FromSeconds(8)), "STA UI thread timed out.");
            return caught;
        }

        private static bool IsReEntrantComboBoxDestruction(string stack) {
            return stack.Contains("ComboBoxChildNativeWindow.WndProc")
                || stack.Contains("ComboBox.WmReflectCommand")
                || stack.Contains("ComboBox.OnSelectedIndexChanged");
        }

        private static void ClickDropdownItem(IntPtr listHwnd, int itemIndex) {
            var rect = new RECT();
            SendMessage(listHwnd, LB_GETITEMRECT, itemIndex, ref rect);
            IntPtr lParam = MakeLParam((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            PostMessage(listHwnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
            PostMessage(listHwnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
            PostMessage(listHwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }

        private static int FindComboIndex(ComboBox combo, string itemText) {
            for (int i = 0; i < combo.Items.Count; i++) {
                if (string.Equals(combo.Items[i]?.ToString(), itemText, StringComparison.Ordinal)) {
                    return i;
                }
            }
            throw new InvalidOperationException($"Item '{itemText}' not found.");
        }

        private static T GetField<T>(object target, string name) {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{name}' not found.");
            return (T)field!.GetValue(target)!;
        }
    }
}
