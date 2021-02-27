namespace Parking.Api.Json.Profiles
{
    public class ProfilePatchRequest
    {
        public ProfilePatchRequest(string alternativeRegistrationNumber, string registrationNumber)
        {
            this.RegistrationNumber = registrationNumber;
            this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
        }
        
        public string AlternativeRegistrationNumber { get; }
        
        public string RegistrationNumber { get; }
    }
}