namespace Parking.Api.Json.Reservations;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class ReservationsPatchRequest(IEnumerable<ReservationsPatchRequestDailyData> reservations)
{
    [Required]
    public IEnumerable<ReservationsPatchRequestDailyData> Reservations { get; } = reservations;
}