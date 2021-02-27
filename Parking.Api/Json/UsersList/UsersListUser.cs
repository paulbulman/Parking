namespace Parking.Api.Json.UsersList
{
    public class UsersListUser
    {
        public UsersListUser(string userId, string name)
        {
            this.UserId = userId;
            this.Name = name;
        }

        public string UserId { get; }

        public string Name { get; }
    }
}