using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.UI.Subsystems.PactMagic;

namespace PlayMore35.UI.Subsystems {
    public class SubsystemUIRegistry {
        public static SubsystemUIRegistry CreateDefault() {
            var registry = new SubsystemUIRegistry();
            registry.Register(new PactMagicUI(), "vestige");
            return registry;
        }

        private readonly List<ISubsystemUI> _providers = new List<ISubsystemUI>();
        private readonly Dictionary<string, ISubsystemUI> _providersById = new Dictionary<string, ISubsystemUI>(StringComparer.OrdinalIgnoreCase);

        public void Register(ISubsystemUI provider, params string[] elementAliases) {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            if (!string.IsNullOrEmpty(provider.SubsystemId)) {
                if (_providersById.TryGetValue(provider.SubsystemId, out var existing)) {
                    _providers.Remove(existing);
                }
                _providersById[provider.SubsystemId] = provider;
            }

            if (elementAliases != null) {
                foreach (var alias in elementAliases) {
                    if (!string.IsNullOrEmpty(alias)) {
                        _providersById[alias] = provider;
                    }
                }
            }

            if (!_providers.Contains(provider)) {
                _providers.Add(provider);
            }
        }

        public ISubsystemUI? GetBySubsystemId(string subsystemId) {
            if (string.IsNullOrEmpty(subsystemId)) return null;
            return _providersById.TryGetValue(subsystemId, out var provider) ? provider : null;
        }

        public ISubsystemUI GetByElementType(string elementType) {
            if (!string.IsNullOrEmpty(elementType) && _providersById.TryGetValue(elementType, out var provider)) {
                return provider;
            }
            return new FallbackSubsystemUI(elementType ?? "");
        }

        public IEnumerable<ISubsystemUI> GetAll() => _providers;

        private class FallbackSubsystemUI : ISubsystemUI {
            public string SubsystemId { get; }

            public FallbackSubsystemUI(string elementType) {
                SubsystemId = elementType;
            }

            public bool IsActive(ICharacterSession session) => false;

            public Control? CreateDedicatedTab(ISubsystemUIHost host) => null;

            public ISubsystemElementPicker CreateElementPicker(ICharacterSession session, ElementPickerContext context) {
                return new DefaultElementPicker(session, context);
            }
        }
    }
}
