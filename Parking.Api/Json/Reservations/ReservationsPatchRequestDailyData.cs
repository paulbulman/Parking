namespace Parking.Api.Json.Reservations;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NodaTime;

public class ReservationsPatchRequestDailyData(LocalDate localDate, IEnumerable<string> userIds)
{
    [Required]
    public LocalDate LocalDate { get; } = localDate;

    [Required]
    public IEnumerable<string> UserIds { get; } = userIds;
}