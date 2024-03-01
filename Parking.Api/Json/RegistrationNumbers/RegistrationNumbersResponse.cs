namespace Parking.Api.Json.RegistrationNumbers;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class RegistrationNumbersResponse(IEnumerable<RegistrationNumbersData> registrationNumbers)
{
    [Required]
    public IEnumerable<RegistrationNumbersData> RegistrationNumbers { get; } = registrationNumbers;
}