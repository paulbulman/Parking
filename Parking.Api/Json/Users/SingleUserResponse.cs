namespace Parking.Api.Json.Users
{
    public class SingleUserResponse
    {
        public SingleUserResponse(UsersData user) => this.User = user;

        public UsersData User { get; }
    }
}