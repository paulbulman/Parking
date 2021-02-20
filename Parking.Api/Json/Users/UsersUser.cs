namespace Parking.Api.Json.Users
{
    public class UsersUser
    {
        public UsersUser(string userId, string name)
        {
            this.UserId = userId;
            this.Name = name;
        }

        public string UserId { get; }

        public string Name { get; }
    }
}