namespace Parking.Api.Json.Users;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class MultipleUsersResponse(IEnumerable<UsersData> users)
{
    [Required]
    public IEnumerable<UsersData> Users { get; } = users;
}