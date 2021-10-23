namespace Parking.Model
{
    public class User
    {
        public User(
            string userId,
            string? alternativeRegistrationNumber,
            decimal? commuteDistance,
            string emailAddress,
            string firstName,
            string lastName,
            string? registrationNumber,
            bool requestReminderEnabled,
            bool reservationReminderEnabled)
        {
            this.UserId = userId;
            this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
            this.CommuteDistance = commuteDistance;
            this.EmailAddress = emailAddress;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.RegistrationNumber = registrationNumber;
            this.RequestReminderEnabled = requestReminderEnabled;
            this.ReservationReminderEnabled = reservationReminderEnabled;
        }

        public string UserId { get; }

        public string? AlternativeRegistrationNumber { get; }

        public decimal? CommuteDistance { get; }

        public string EmailAddress { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string? RegistrationNumber { get; }

        public bool RequestReminderEnabled { get; }

        public bool ReservationReminderEnabled { get; }
    }
}