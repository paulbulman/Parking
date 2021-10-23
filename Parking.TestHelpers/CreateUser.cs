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
            string? registrationNumber = null,
            bool requestReminderEnabled = true,
            bool reservationReminderEnabled = true) =>
            new User(
                userId: userId,
                alternativeRegistrationNumber: alternativeRegistrationNumber,
                commuteDistance: commuteDistance,
                emailAddress: emailAddress,
                firstName: firstName,
                lastName: lastName,
                registrationNumber: registrationNumber,
                requestReminderEnabled: requestReminderEnabled,
                reservationReminderEnabled: reservationReminderEnabled);
    }
}