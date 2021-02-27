namespace Parking.Api.Json.Users
{
    public class UserPatchRequest
    {
        public UserPatchRequest(
            string alternativeRegistrationNumber,
            decimal? commuteDistance,
            string firstName,
            string lastName,
            string registrationNumber)
        {
            this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
            this.CommuteDistance = commuteDistance;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.RegistrationNumber = registrationNumber;
        }
        
        public string AlternativeRegistrationNumber { get; }
        
        public decimal? CommuteDistance { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }
        
        public string RegistrationNumber { get; }
    }
}