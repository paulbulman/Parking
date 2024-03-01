namespace Parking.Api.Json.Reservations;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Calendar;

public class ReservationsResponse(
    Calendar<ReservationsData> reservations,
    int shortLeadTimeSpaces,
    IEnumerable<ReservationsUser> users)
{
    [Required]
    public Calendar<ReservationsData> Reservations { get; } = reservations;

    [Required]
    public int ShortLeadTimeSpaces { get; } = shortLeadTimeSpaces;

    [Required]
    public IEnumerable<ReservationsUser> Users { get; } = users;
}