using System;

namespace Scheduling
{
    /// <summary>
    /// This class extends the C# DateTime with some functionality known from Java
    /// </summary>
    public static class DateTimeExtensions
    {
        public static DateTime Add(this DateTime timestamp, int value, TimeUnit unit)
        {
            const int daysPerWeek = 7;
            return unit switch
            {
                TimeUnit.SECONDS => timestamp.AddSeconds(value),
                TimeUnit.MINUTES => timestamp.AddMinutes(value),
                TimeUnit.HOURS => timestamp.AddHours(value),
                TimeUnit.DAYS => timestamp.AddDays(value),
                TimeUnit.WEEKS => timestamp.AddDays(daysPerWeek * value),
                TimeUnit.MONTHS => timestamp.AddMonths(value),
                TimeUnit.YEARS => timestamp.AddYears(value),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}