using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace FastQ.Web.Helpers
{
    public static class Utilities
    {
        public enum DayofWeekType
        {
            Short = 0,
            Long = 1
        }
        public static string TranslateToDayOfWeek(int daynum, DayofWeekType fmt)
        {
            if (fmt.Equals(DayofWeekType.Short)) {
                return DateTimeFormatInfo.CurrentInfo.AbbreviatedDayNames[daynum];
            }
            else if (fmt.Equals(DayofWeekType.Long))
            {
                return DateTimeFormatInfo.CurrentInfo.DayNames[daynum];
            }
            return null;
        }

        public static string TranslateLeadTimes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.EndsWith("D"))
                return input.Replace("PT", "").Replace("P", "").Replace("D", " day");
            else if (input.EndsWith("H"))
                return input.Replace("PT", "").Replace("P", "").Replace("H", " hours");
            else
                return input;
        }
    }
}