namespace Parking.Api.Json.DailyDetails;

using System.ComponentModel.DataAnnotations;
using NodaTime;

public class StayInterruptedPatchRequest(LocalDate localDate, bool stayInterrupted)
{
    [Required]
    public LocalDate LocalDate { get; } = localDate;

    [Required]
    public bool StayInterrupted { get; } = stayInterrupted;
}