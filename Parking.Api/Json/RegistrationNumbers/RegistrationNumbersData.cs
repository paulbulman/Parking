namespace Parking.Api.Json.RegistrationNumbers
{
    public class RegistrationNumbersData
    {
        public RegistrationNumbersData(string registrationNumber, string name)
        {
            this.RegistrationNumber = registrationNumber;
            this.Name = name;
        }

        public string RegistrationNumber { get; }
        
        public string Name { get; }
    }
}