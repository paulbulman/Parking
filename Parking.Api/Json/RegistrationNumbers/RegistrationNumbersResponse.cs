namespace Parking.Api.Json.RegistrationNumbers
{
    using System.Collections.Generic;

    public class RegistrationNumbersResponse
    {
        public RegistrationNumbersResponse(IEnumerable<RegistrationNumbersData> registrationNumbers) =>
            this.RegistrationNumbers = registrationNumbers;

        public IEnumerable<RegistrationNumbersData> RegistrationNumbers { get; }
    }
}