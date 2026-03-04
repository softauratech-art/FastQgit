using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;

namespace FastQ.Web.Helpers
{
    public static class Utilities
    {
        public enum DayofWeekType
        {
            Short = 0,
            Long = 1
        }

        public static string ParseTimestampForDB(string ts)
        {
            string dbts;
            try
            {
                dbts = TimeSpan.TryParse(ts, out TimeSpan result) ? ts : dbts = DateTime.ParseExact(ts, "h:mm tt", System.Globalization.CultureInfo.InvariantCulture).TimeOfDay.ToString();
                dbts = !dbts.StartsWith("00 ") ? "00 " + dbts : dbts;
                return dbts;
            }
            catch
            {
                return null;
            }
        }
        public static string ParseDurationFromISO(string isoduration)
        {
            if (string.IsNullOrEmpty(isoduration)) return isoduration;

            //return string in dd hh:mi:ss format
            TimeSpan timeSpan = System.Xml.XmlConvert.ToTimeSpan(isoduration);
            string ts = timeSpan.ToString();
            //ts = ts.Contains(".") ? ts.Replace(".", " ") : ts;  //replace . with [space] for Oracle format
            ts = string.Format("{0:00} {1:00}:{2:00}:{3:00}", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

            return ts;
        }

        public static string ParseTimeInterval(string rawInterval)
        {
            if (string.IsNullOrEmpty(rawInterval) || rawInterval.Trim().Length == 0) return rawInterval;

            try
            {   // ex: "+90 00:00:00.000000";
                TimeSpan ts = TimeSpan.Parse(rawInterval);
                // Custom Readable String (e.g., "90 Days")
                string friendly2 = $"{ts.Days} Days, {ts.Hours} Hours"; // Output: 90 Days, 0 Hours
                return friendly2;
            }
            catch (Exception ex)
            {
                if (rawInterval.EndsWith("D"))
                    return rawInterval.Replace("PT", "").Replace("P", "").Replace("D", " days");
                else if (rawInterval.EndsWith("H"))
                    return rawInterval.Replace("PT", "").Replace("P", "").Replace("H", " hours");
                else if (rawInterval.EndsWith("M"))
                    return rawInterval.Replace("PT", "").Replace("P", "").Replace("M", " minutes");
                else
                    return rawInterval;
            }
        }
        public static string TranslateToDayOfWeek(int daynum, DayofWeekType fmt)
        {
            if (fmt.Equals(DayofWeekType.Short))
            {
                return DateTimeFormatInfo.CurrentInfo.AbbreviatedDayNames[daynum];
            }
            else if (fmt.Equals(DayofWeekType.Long))
            {
                return DateTimeFormatInfo.CurrentInfo.DayNames[daynum];
            }
            return null;
        }

        public static string WeeklySchPrettyPrint(string weeklynumsch, string spacer)
        {
            //input: 234 [0-6 ie. Sun-Sat]
            IEnumerable<int> digits = weeklynumsch.Select(c => (int)Char.GetNumericValue(c));
            string friendly1 = string.Empty;
            foreach (int i in digits)
            {
                friendly1 += FastQ.Web.Helpers.Utilities.TranslateToDayOfWeek(i, Helpers.Utilities.DayofWeekType.Short) + spacer;
            }
            return friendly1;

        }

    }

}