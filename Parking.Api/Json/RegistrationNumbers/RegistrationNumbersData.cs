namespace Parking.Api.Json.RegistrationNumbers
{
    using System.ComponentModel.DataAnnotations;

    public class RegistrationNumbersData
    {
        public RegistrationNumbersData(string registrationNumber, string name)
        {
            this.RegistrationNumber = registrationNumber;
            this.Name = name;
        }

        [Required]
        public string RegistrationNumber { get; }
        
        [Required]
        public string Name { get; }
    }
}