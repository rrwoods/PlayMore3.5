using System.Collections.Generic;

namespace PlayMore35.Global {
    public class StatView {
        public string Name { get; }
        public int Value { get; }
        public IReadOnlyList<string> Explanation { get; }

        public StatView(string name, int value, IReadOnlyList<string> explanation = null) {
            Name = name ?? "";
            Value = value;
            Explanation = explanation ?? new List<string>().AsReadOnly();
        }
    }
}
