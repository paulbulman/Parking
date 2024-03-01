namespace Parking.Api.Json.Calendar;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Calendar<T>(IEnumerable<Week<T>> weeks)
    where T : class
{
    [Required]
    public IEnumerable<Week<T>> Weeks { get; } = weeks;
}