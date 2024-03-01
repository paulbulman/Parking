namespace Parking.Api.Json.Users;

using System.ComponentModel.DataAnnotations;

public class SingleUserResponse
{
    public SingleUserResponse(UsersData user) => this.User = user;

    [Required]
    public UsersData User { get; }
}