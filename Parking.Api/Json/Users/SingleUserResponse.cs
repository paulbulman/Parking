namespace Parking.Api.Json.Users;

using System.ComponentModel.DataAnnotations;

public class SingleUserResponse(UsersData user)
{
    [Required]
    public UsersData User { get; } = user;
}