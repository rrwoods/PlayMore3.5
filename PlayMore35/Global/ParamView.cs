namespace PlayMore35.Global {
    public class ParamView {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public int Value { get; }

        public ParamView(string name, string category, string description, int value) {
            Name = name ?? "";
            Category = category ?? "";
            Description = description ?? "";
            Value = value;
        }
    }
}
