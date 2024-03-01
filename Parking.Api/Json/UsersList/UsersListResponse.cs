namespace Parking.Api.Json.UsersList;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class UsersListResponse
{
    public UsersListResponse(IEnumerable<UsersListUser> users) => this.Users = users;

    [Required]
    public IEnumerable<UsersListUser> Users { get; }
}