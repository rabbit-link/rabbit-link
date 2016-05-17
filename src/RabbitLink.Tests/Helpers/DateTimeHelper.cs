#region Usings

using System;

#endregion

namespace RabbitLink.Tests.Helpers
{
    internal static class DateTimeHelper
    {
        public static DateTime FromUnixTime(this long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0)
                .AddSeconds(unixTime);
        }

        public static long ToUnixTime(this DateTime dateTime)
        {
            var diff = dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (long) Math.Floor(diff.TotalSeconds);
        }
    }
}