using System;
using System.Collections.Generic;
using PlayMore35.Effects;

namespace PlayMore35.Global {
    public class ToggleView {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public bool IsActive { get; }
        public bool IsVisible { get; }

        public bool HasResource { get; }
        public bool IsAtWill { get; }
        public string ResourceDescription { get; }
        public int MaxUses { get; }
        public IReadOnlyList<string> UsesExplanation { get; }
        public string EffectDuration { get; }
        public string RechargeDuration { get; }
        public SheetAction ActionAfter { get; }

        public ToggleView(
            string name,
            string category,
            string description,
            bool isActive,
            bool isVisible,
            bool hasResource = false,
            bool isAtWill = false,
            string resourceDescription = "",
            int maxUses = 0,
            IReadOnlyList<string> usesExplanation = null,
            string effectDuration = null,
            string rechargeDuration = null,
            SheetAction actionAfter = null
        ) {
            Name = name ?? "";
            Category = category ?? "";
            Description = description ?? "";
            IsActive = isActive;
            IsVisible = isVisible;
            HasResource = hasResource;
            IsAtWill = isAtWill;
            ResourceDescription = resourceDescription ?? "";
            MaxUses = maxUses;
            UsesExplanation = usesExplanation ?? new string[0];
            EffectDuration = effectDuration;
            RechargeDuration = rechargeDuration;
            ActionAfter = actionAfter;
        }

        public ToggleView(string name, string category, string description, bool isActive, bool isVisible, ActionResource resource)
            : this(
                name,
                category,
                description,
                isActive,
                isVisible,
                hasResource: resource != null,
                isAtWill: resource != null && resource.RechargeTime?.unit == Constants.AT_WILL,
                resourceDescription: resource != null ? resource.Description() : "",
                maxUses: resource != null ? resource.DynamicUses : 0,
                usesExplanation: resource != null && resource.DynamicUsesExplanation != null ? resource.DynamicUsesExplanation.AsReadOnly() : null,
                effectDuration: resource?.Duration,
                rechargeDuration: (resource != null && resource.RechargeTime != null && !resource.RechargeTime.quantity.IsSingleOne())
                    ? resource.RechargeTime.ToString()
                    : null,
                actionAfter: resource?.ActionAfter
            ) { }
    }
}
