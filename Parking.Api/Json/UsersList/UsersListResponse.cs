namespace Parking.Api.Json.UsersList;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class UsersListResponse(IEnumerable<UsersListUser> users)
{
    [Required]
    public IEnumerable<UsersListUser> Users { get; } = users;
}