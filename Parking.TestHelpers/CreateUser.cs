namespace Parking.TestHelpers
{
    using Model;

    public static class CreateUser
    {
        public static User With(
            string userId,
            string? alternativeRegistrationNumber = null,
            decimal? commuteDistance = null,
            string emailAddress = "john.doe@example.com",
            string firstName = "John",
            string lastName = "Doe",
            string? registrationNumber = null) =>
            new User(
                userId,
                alternativeRegistrationNumber,
                commuteDistance,
                emailAddress,
                firstName,
                lastName,
                registrationNumber);
    }
}