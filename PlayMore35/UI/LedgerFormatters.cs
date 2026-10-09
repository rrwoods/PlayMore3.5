using System;

namespace PlayMore35.UI {
    public static class LedgerFormatters {
        public const string GAME_TIME_FORMAT = "yyyy-MM-dd HH:mm";
        public const string REAL_DATE_FORMAT = "yyyy-MM-dd";

        public static string FormatGameTime(DateTime time) => time.ToString(GAME_TIME_FORMAT);

        public static string FormatRealDate(DateTime date) => date.ToString(REAL_DATE_FORMAT);

        public static string FormatGoldAmount(decimal amount) => amount.ToString("+#,##0.##;-#,##0.##;0");

        public static string FormatTotalGold(decimal amount) => amount.ToString("#,###.##;-#,###.##;0");

        public static string FormatXpAmount(int xp) => xp.ToString("+#,###;-#,###;0");

        public static string FormatTotalXp(int xp) => xp.ToString("#,###;-#,###;0");
    }
}
