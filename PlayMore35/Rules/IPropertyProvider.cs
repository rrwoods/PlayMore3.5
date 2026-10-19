using System.Collections.Generic;

namespace PlayMore35.Rules {
    public interface IPropertyProvider {
        IReadOnlyDictionary<string, object> Properties { get; }
    }
}
