namespace Parking.Model
{
    public class User
    {
        public User(string userId, decimal? commuteDistance, string emailAddress, string firstName, string lastName)
        {
            UserId = userId;
            CommuteDistance = commuteDistance;
            EmailAddress = emailAddress;
            FirstName = firstName;
            LastName = lastName;
        }

        public string UserId { get; }

        public decimal? CommuteDistance { get; }
        
        public string EmailAddress { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }
    }
}