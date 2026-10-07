using PlayMore35.Effects;

namespace PlayMore35.Global {
    public class ToggleView {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public bool IsActive { get; }
        public bool IsVisible { get; }
        public ActionResource Resource { get; }

        public ToggleView(string name, string category, string description, bool isActive, bool isVisible, ActionResource resource = null) {
            Name = name ?? "";
            Category = category ?? "";
            Description = description ?? "";
            IsActive = isActive;
            IsVisible = isVisible;
            Resource = resource;
        }
    }
}
