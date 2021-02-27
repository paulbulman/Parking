namespace Parking.Api.Json.UsersList
{
    using System.Collections.Generic;

    public class UsersListResponse
    {
        public UsersListResponse(IEnumerable<UsersListUser> users) => this.Users = users;

        public IEnumerable<UsersListUser> Users { get; }
    }
}