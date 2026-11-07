using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.CharacterChoices {
    public class XpSessionEntry : IJsonable, IEquatable<XpSessionEntry> {
        public int Xp { get; }
        public string Description { get; }
        public DateTime GameTime { get; }
        public DateTime RealDate { get; }

        public XpSessionEntry(int xp, string description, DateTime gameTime, DateTime realDate) {
            Xp = xp;
            Description = description ?? "";
            GameTime = gameTime;
            RealDate = realDate.Date;
        }

        public XpSessionEntry(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));

            Xp = (int)json["xp"];
            Description = (string)json["description"] ?? "";
            GameTime = DateTime.Parse((string)json["game_time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            RealDate = DateTime.Parse((string)json["real_date"], CultureInfo.InvariantCulture).Date;
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("xp", Xp),
                new JProperty("description", Description),
                new JProperty("game_time", GameTime.ToString("O")),
                new JProperty("real_date", RealDate.ToString("yyyy-MM-dd"))
            );
        }

        public bool Equals(XpSessionEntry other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Xp == other.Xp &&
                   Description == other.Description &&
                   GameTime.Equals(other.GameTime) &&
                   RealDate.Equals(other.RealDate);
        }

        public override bool Equals(object obj) {
            return Equals(obj as XpSessionEntry);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + Xp.GetHashCode();
                hash = hash * 31 + (Description != null ? Description.GetHashCode() : 0);
                hash = hash * 31 + GameTime.GetHashCode();
                hash = hash * 31 + RealDate.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return $"{Xp:+#,###;-#,###;0} XP: {Description} (Game: {GameTime:yyyy-MM-dd HH:mm}, Real: {RealDate:yyyy-MM-dd})";
        }
    }
}
