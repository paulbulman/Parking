namespace Parking.Api.Json.Calendar;

using System.Collections.Generic;
using System.Linq;
using Business;
using NodaTime;

public static class Helpers
{
    public static Calendar<T> CreateCalendar<T>(IDictionary<LocalDate, T> data) where T : class
    {
        var weeks = data.Keys
            .Select(d => d.StartOfWeek())
            .Distinct()
            .Select(d => CreateWeek(data, d));

        return new Calendar<T>(weeks);
    }

    private static Week<T> CreateWeek<T>(IDictionary<LocalDate, T> data, LocalDate weekStart) where T : class
    {
        var days = Enumerable
            .Range(0, 5)
            .Select(offset => CreateDay(data, weekStart.PlusDays(offset)));

        return new Week<T>(days);
    }
        
    private static Day<T> CreateDay<T>(IDictionary<LocalDate, T> data, LocalDate localDate) where T : class =>
        data.ContainsKey(localDate)
            ? new Day<T>(localDate, data[localDate])
            : Day<T>.CreateHidden(localDate);
}