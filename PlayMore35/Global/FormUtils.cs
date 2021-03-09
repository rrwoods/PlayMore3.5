using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayMore35.Global {
    public class FormUtils {
        public static List<string> availableFeats;

        public static void ClearDynamicControls<T>(Control container, List<T> controls) where T : Control {
            foreach (T control in controls) {
                container.Controls.Remove(control);
            }
            controls.Clear();
        }

        public static void ClearDynamicControls<K, T>(Control container, Dictionary<K, List<T>> controls) where T : Control {
            foreach (var category in controls) {
                foreach (Control control in category.Value) {
                    container.Controls.Remove(control);
                }
            }
            controls.Clear();
        }
    }
}
