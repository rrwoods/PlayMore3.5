using System;
using System.Collections.Generic;
using System.Reflection;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Tests {
    internal static class DomainInspectionTestExtensions {
        public static IEnumerable<Effect> GetEffects(this EffectPool pool) {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            var field = typeof(EffectPool).GetField("effects", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new InvalidOperationException("Field 'effects' not found on EffectPool.");
            return (IEnumerable<Effect>)field.GetValue(pool);
        }

        public static IReadOnlyDictionary<string, string> GetRememberedFeats(this CharacterSession session) {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var field = typeof(CharacterSession).GetField("rememberedFeats", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new InvalidOperationException("Field 'rememberedFeats' not found on CharacterSession.");
            return (IReadOnlyDictionary<string, string>)field.GetValue(session);
        }

        public static IReadOnlyDictionary<string, string> GetRememberedFeatChoices(this CharacterSession session) {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var field = typeof(CharacterSession).GetField("rememberedFeatChoices", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new InvalidOperationException("Field 'rememberedFeatChoices' not found on CharacterSession.");
            return (IReadOnlyDictionary<string, string>)field.GetValue(session);
        }
    }
}
