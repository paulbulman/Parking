namespace Parking.Api.Json.Users
{
    using System.ComponentModel.DataAnnotations;

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
        
        [Required]
        public string EmailAddress { get; }

        [Required]
        public string FirstName { get; }
        
        [Required]
        public string LastName { get; }
        
        public string? RegistrationNumber { get; }
    }
}