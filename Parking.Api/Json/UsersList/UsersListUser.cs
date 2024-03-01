namespace Parking.Api.Json.UsersList;

using System.ComponentModel.DataAnnotations;

public class UsersListUser
{
    public UsersListUser(string userId, string name)
    {
        this.UserId = userId;
        this.Name = name;
    }

    [Required]
    public string UserId { get; }

    [Required]
    public string Name { get; }
}