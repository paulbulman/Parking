namespace Parking.Api.Json.UsersList;

using System.ComponentModel.DataAnnotations;

public class UsersListUser(string userId, string name)
{
    [Required]
    public string UserId { get; } = userId;

    [Required]
    public string Name { get; } = name;
}