namespace Parking.Api.Json.Users
{
    using System.ComponentModel.DataAnnotations;

    public class UsersData
    {
        public UsersData(string userId, string? alternativeRegistrationNumber, decimal? commuteDistance, string firstName, string lastName, string? registrationNumber)
        {
            this.UserId = userId;
            this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
            this.CommuteDistance = commuteDistance;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.RegistrationNumber = registrationNumber;
        }
        
        [Required]
        public string UserId { get; }
        
        public string? AlternativeRegistrationNumber { get; }
        
        public decimal? CommuteDistance { get; }
        
        [Required]
        public string FirstName { get; }
        
        [Required]
        public string LastName { get; }
        
        public string? RegistrationNumber { get; }
    }
}