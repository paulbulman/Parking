namespace Parking.Api.Json.Users
{
    using System.Collections.Generic;

    public class MultipleUsersResponse
    {
        public MultipleUsersResponse(IEnumerable<UsersData> users) => this.Users = users;

        public IEnumerable<UsersData> Users { get; }
    }
}