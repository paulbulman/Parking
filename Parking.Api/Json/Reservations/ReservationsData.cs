namespace Parking.Api.Json.Reservations;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class ReservationsData(IEnumerable<string> userIds)
{
    [Required]
    public IEnumerable<string> UserIds { get; } = userIds;
}