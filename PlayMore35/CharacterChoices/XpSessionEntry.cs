using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.CharacterChoices {
    public class XpSessionEntry : IJsonable, IEquatable<XpSessionEntry> {
        public int Xp { get; }
        public string Description { get; }
        public long GameRound { get; }
        public DateTime RealDate { get; }

        public XpSessionEntry(int xp, string description, long gameRound, DateTime realDate) {
            Xp = xp;
            Description = description ?? "";
            GameRound = gameRound;
            RealDate = realDate.Date;
        }

        public XpSessionEntry(JObject json) : this(
            json != null ? (int)json["xp"] : throw new ArgumentNullException(nameof(json)),
            (string)json["description"] ?? "",
            (long)json["game_round"],
            DateTime.Parse((string)json["real_date"], CultureInfo.InvariantCulture).Date
        ) { }

        public JToken ToJson() {
            return new JObject(
                new JProperty("xp", Xp),
                new JProperty("description", Description),
                new JProperty("game_round", GameRound),
                new JProperty("real_date", RealDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            );
        }

        public bool Equals(XpSessionEntry other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Xp == other.Xp &&
                   Description == other.Description &&
                   GameRound == other.GameRound &&
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
                hash = hash * 31 + GameRound.GetHashCode();
                hash = hash * 31 + RealDate.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return $"{Xp:+#,###;-#,###;0} XP: {Description} (Round: {GameRound}, Real: {RealDate:yyyy-MM-dd})";
        }
    }
}
