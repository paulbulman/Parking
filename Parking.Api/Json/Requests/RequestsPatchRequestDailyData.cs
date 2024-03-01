namespace Parking.Api.Json.Requests;

using System.ComponentModel.DataAnnotations;
using NodaTime;

public class RequestsPatchRequestDailyData(LocalDate localDate, bool requested)
{
    [Required]
    public LocalDate LocalDate { get; } = localDate;

    [Required]
    public bool Requested { get; } = requested;
}