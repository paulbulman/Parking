namespace Parking.Api.Json.Users;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class MultipleUsersResponse
{
    public MultipleUsersResponse(IEnumerable<UsersData> users) => this.Users = users;

    [Required]
    public IEnumerable<UsersData> Users { get; }
}