using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.CharacterChoices {
    public class GpTransaction : IJsonable, IEquatable<GpTransaction> {
        public decimal Amount { get; }
        public string Description { get; }
        public long GameRound { get; }
        public DateTime RealDate { get; }

        public GpTransaction(decimal amount, string description, long gameRound, DateTime realDate) {
            Amount = amount;
            Description = description ?? "";
            GameRound = gameRound;
            RealDate = realDate.Date;
        }

        public GpTransaction(JObject json) : this(
            json != null ? (decimal)json["amount"] : throw new ArgumentNullException(nameof(json)),
            (string)json["description"] ?? "",
            (long)json["game_round"],
            DateTime.Parse((string)json["real_date"], CultureInfo.InvariantCulture).Date
        ) { }

        public JToken ToJson() {
            return new JObject(
                new JProperty("amount", Amount),
                new JProperty("description", Description),
                new JProperty("game_round", GameRound),
                new JProperty("real_date", RealDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            );
        }

        public bool Equals(GpTransaction other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Amount == other.Amount &&
                   Description == other.Description &&
                   GameRound == other.GameRound &&
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
                hash = hash * 31 + GameRound.GetHashCode();
                hash = hash * 31 + RealDate.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return $"{Amount:+#,##0.##;-#,##0.##;0} GP: {Description} (Round: {GameRound}, Real: {RealDate:yyyy-MM-dd})";
        }
    }
}
