namespace Parking.Api.Json.Calendar;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Week<T>(IEnumerable<Day<T>> days)
    where T : class
{
    [Required]
    public IEnumerable<Day<T>> Days { get; } = days;
}