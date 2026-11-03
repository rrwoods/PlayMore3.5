using System;
using System.Collections.Generic;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Global {
    public enum ProgressionChoiceDomain {
        Feat,
        Subsystem,
        Class
    }

    public class ProgressionChoiceContext {
        public ProgressionChoiceDomain Domain { get; set; }
        public int AcquisitionLevel { get; set; }
        public string? SlotKey { get; set; }
        public string ChoiceValue { get; set; } = "";
        public string? TargetSource { get; set; }
        public ChoiceDescriptor? Descriptor { get; set; }
        public IReadOnlyDictionary<string, string>? CurrentChoices { get; set; }

        public ProgressionChoiceContext() { }

        public ProgressionChoiceContext(
            ProgressionChoiceDomain domain,
            string choiceValue,
            int acquisitionLevel = 0,
            string? slotKey = null,
            string? targetSource = null,
            ChoiceDescriptor? descriptor = null,
            IReadOnlyDictionary<string, string>? currentChoices = null) {
            Domain = domain;
            ChoiceValue = choiceValue;
            AcquisitionLevel = acquisitionLevel;
            SlotKey = slotKey;
            TargetSource = targetSource;
            Descriptor = descriptor;
            CurrentChoices = currentChoices;
        }
    }
}
