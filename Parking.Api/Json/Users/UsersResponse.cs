namespace Parking.Api.Json.Users
{
    using System.Collections.Generic;

    public class UsersResponse
    {
        public UsersResponse(IEnumerable<UsersUser> users) => this.Users = users;

        public IEnumerable<UsersUser> Users { get; }
    }
}