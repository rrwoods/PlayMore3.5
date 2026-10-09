using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.CharacterChoices {
    public class GpTransaction : IJsonable, IComparable<GpTransaction>, IEquatable<GpTransaction> {
        public decimal Amount { get; }
        public string Description { get; }
        public DateTime GameTime { get; }
        public DateTime RealDate { get; }

        public GpTransaction(decimal amount, string description, DateTime gameTime, DateTime realDate) {
            Amount = amount;
            Description = description ?? "";
            GameTime = gameTime;
            RealDate = realDate.Date;
        }

        public GpTransaction(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));

            Amount = (decimal)json["amount"];
            Description = (string)json["description"] ?? "";
            GameTime = DateTime.Parse((string)json["game_time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            RealDate = DateTime.Parse((string)json["real_date"], CultureInfo.InvariantCulture).Date;
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("amount", Amount),
                new JProperty("description", Description),
                new JProperty("game_time", GameTime.ToString("O")),
                new JProperty("real_date", RealDate.ToString("yyyy-MM-dd"))
            );
        }

        public int CompareTo(GpTransaction other) {
            if (other == null) return 1;
            int cmp = GameTime.CompareTo(other.GameTime);
            if (cmp != 0) return cmp;
            return RealDate.CompareTo(other.RealDate);
        }

        public bool Equals(GpTransaction other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Amount == other.Amount &&
                   Description == other.Description &&
                   GameTime.Equals(other.GameTime) &&
                   RealDate.Equals(other.RealDate);
        }

        public override bool Equals(object obj) {
            return Equals(obj as GpTransaction);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + Amount.GetHashCode();
                hash = hash * 31 + (Description != null ? Description.GetHashCode() : 0);
                hash = hash * 31 + GameTime.GetHashCode();
                hash = hash * 31 + RealDate.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return $"{Amount:+#,##0.##;-#,##0.##;0} GP: {Description} (Game: {GameTime:yyyy-MM-dd HH:mm}, Real: {RealDate:yyyy-MM-dd})";
        }
    }
}
