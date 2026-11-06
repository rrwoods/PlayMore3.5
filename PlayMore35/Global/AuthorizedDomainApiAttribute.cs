using System;

namespace PlayMore35.Global {
    /// <summary>
    /// Identifies an authorized public domain contract or headless entry point in the rules engine
    /// that is intentionally exposed for headless execution, scripting, or external callers
    /// beyond direct WinForms UI invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
    public sealed class AuthorizedDomainApiAttribute : Attribute {
        public string? Reason { get; }

        public AuthorizedDomainApiAttribute(string? reason = null) {
            Reason = reason;
        }
    }
}
