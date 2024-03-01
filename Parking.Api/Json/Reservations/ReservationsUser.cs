namespace Parking.Api.Json.Reservations;

using System.ComponentModel.DataAnnotations;

public class ReservationsUser(string userId, string name)
{
    [Required]
    public string UserId { get; } = userId;

    [Required]
    public string Name { get; } = name;
}