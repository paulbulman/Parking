namespace Parking.Model
{
    public class User
    {
        public User(string userId, decimal? commuteDistance, string emailAddress)
        {
            UserId = userId;
            CommuteDistance = commuteDistance;
            EmailAddress = emailAddress;
        }

        public string UserId { get; }

        public decimal? CommuteDistance { get; }
        
        public string EmailAddress { get; }
    }
}