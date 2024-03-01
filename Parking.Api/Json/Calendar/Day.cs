namespace Parking.Api.Json.Calendar;

using System.ComponentModel.DataAnnotations;
using NodaTime;

public class Day<T> where T : class
{
    public Day(LocalDate localDate, T data) : this(localDate, data, hidden: false)
    {
    }

    private Day(LocalDate localDate, T? data, bool hidden)
    {
        this.LocalDate = localDate;
        this.Data = data;
        this.Hidden = hidden;
    }

    public static Day<T> CreateHidden(LocalDate localDate) => new Day<T>(localDate, null, hidden: true);

    [Required]
    public LocalDate LocalDate { get; }
        
    public T? Data { get; }

    [Required]
    public bool Hidden { get; }
}