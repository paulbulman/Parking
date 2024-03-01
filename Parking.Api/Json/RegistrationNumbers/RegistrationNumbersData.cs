namespace Parking.Api.Json.RegistrationNumbers;

using System.ComponentModel.DataAnnotations;

public class RegistrationNumbersData(string registrationNumber, string name)
{
    [Required]
    public string RegistrationNumber { get; } = registrationNumber;

    [Required]
    public string Name { get; } = name;
}