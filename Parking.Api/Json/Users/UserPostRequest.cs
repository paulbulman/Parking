namespace Parking.Api.Json.Users
{
    public class UserPostRequest
    {
        public UserPostRequest(
            string? alternativeRegistrationNumber,
            decimal? commuteDistance,
            string emailAddress,
            string firstName,
            string lastName,
            string? registrationNumber)
        {
            this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
            this.CommuteDistance = commuteDistance;
            this.EmailAddress = emailAddress;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.RegistrationNumber = registrationNumber;
        }
        
        public string? AlternativeRegistrationNumber { get; }
        
        public decimal? CommuteDistance { get; }
        
        public string EmailAddress { get; }

        public string FirstName { get; }
        
        public string LastName { get; }
        
        public string? RegistrationNumber { get; }
    }
}