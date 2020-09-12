namespace ParkingService.Model
{
    public class User
    {
        public User(string userId, decimal commuteDistance)
        {
            UserId = userId;
            CommuteDistance = commuteDistance;
        }

        public string UserId { get; }

        public decimal CommuteDistance { get; }
    }
}