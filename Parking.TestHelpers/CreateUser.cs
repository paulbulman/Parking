namespace Parking.TestHelpers
{
    using Model;

    public static class CreateUser
    {
        public static User With(
            string userId,
            decimal? commuteDistance = null,
            string emailAddress = "john.doe@example.com") =>
            new User(userId, commuteDistance, emailAddress);
    }
}