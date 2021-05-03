namespace Parking.Api.UnitTests.Json.Calendar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Api.Json.Calendar;
    using NodaTime;

    public static class CalendarHelpers
    {
        public static IReadOnlyCollection<Day<T>> GetVisibleDays<T>(Calendar<T> calendar) where T : class =>
            calendar.Weeks
                .SelectMany(w => w.Days)
                .Where(d => !d.Hidden)
                .ToArray();

        public static T GetDailyData<T>(Calendar<T> calendar, LocalDate localDate) where T : class
        {
            var day = calendar.Weeks
                .SelectMany(w => w.Days)
                .Single(d => d.LocalDate == localDate);

            if (day.Data == null)
            {
                throw new InvalidOperationException("No data was found for the requested day.");
            }

            return day.Data;
        }
    }
}