namespace Parking.Api.Json.RegistrationNumbers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class RegistrationNumbersResponse
    {
        public RegistrationNumbersResponse(IEnumerable<RegistrationNumbersData> registrationNumbers) =>
            this.RegistrationNumbers = registrationNumbers;

        [Required]
        public IEnumerable<RegistrationNumbersData> RegistrationNumbers { get; }
    }
}