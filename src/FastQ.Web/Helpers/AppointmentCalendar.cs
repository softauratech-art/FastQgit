using System;
using System.Globalization;
using System.Text;

namespace FastQ.Web.Helpers
{
    internal static class AppointmentCalendar
    {
        public static string Build(long id, DateTime start, TimeSpan? endTime,
            string summary, string description, string location, string sourceType = "A")
        {
            // FastQ appointments use Orange County wall-clock time, independent
            // of the web server's local time zone. UTC makes Outlook imports portable.
            TimeZoneInfo eastern;
            try { eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
            catch (TimeZoneNotFoundException) { eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
            start = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
            var end = endTime.HasValue ? start.Date + endTime.Value : start;
            if (endTime.HasValue && end < start) end = end.AddDays(1);
            var text = new StringBuilder();
            Add(text, "BEGIN:VCALENDAR");
            Add(text, "VERSION:2.0");
            Add(text, "PRODID:-//Orange County//FastQ//EN");
            Add(text, "CALSCALE:GREGORIAN");
            Add(text, "METHOD:PUBLISH");
            Add(text, "BEGIN:VEVENT");
            Add(text, "UID:fastq-" + (sourceType == "W" ? "walkin" : "appointment") + "-" + id.ToString(CultureInfo.InvariantCulture) + "@ocfl.net");
            Add(text, "DTSTAMP:" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
            Add(text, "DTSTART:" + TimeZoneInfo.ConvertTimeToUtc(start, eastern).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
            if (end > start)
                Add(text, "DTEND:" + TimeZoneInfo.ConvertTimeToUtc(end, eastern).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
            Add(text, "SUMMARY:" + Escape(summary));
            Add(text, "DESCRIPTION:" + Escape(description));
            Add(text, "LOCATION:" + Escape(location));
            Add(text, "END:VEVENT");
            Add(text, "END:VCALENDAR");
            return text.ToString();
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\")
                .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n")
                .Replace(";", "\\;").Replace(",", "\\,");
        }

        private static void Add(StringBuilder text, string line)
        {
            // iCalendar lines are limited to 75 UTF-8 octets; preserve surrogate pairs.
            var bytes = 0;
            for (var i = 0; i < line.Length; i++)
            {
                var length = char.IsHighSurrogate(line[i]) && i + 1 < line.Length
                    && char.IsLowSurrogate(line[i + 1]) ? 2 : 1;
                var character = line.Substring(i, length);
                var size = Encoding.UTF8.GetByteCount(character);
                if (bytes + size > 75) { text.Append("\r\n "); bytes = 1; }
                text.Append(character);
                bytes += size;
                i += length - 1;
            }
            text.Append("\r\n");
        }
    }
}
