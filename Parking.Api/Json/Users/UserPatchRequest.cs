﻿namespace Parking.Api.Json.Users;

using System.ComponentModel.DataAnnotations;

public class UserPatchRequest(
    string? alternativeRegistrationNumber,
    decimal? commuteDistance,
    string firstName,
    string lastName,
    string? registrationNumber)
{
    public string? AlternativeRegistrationNumber { get; } = alternativeRegistrationNumber;

    public decimal? CommuteDistance { get; } = commuteDistance;

    [Required]
    public string FirstName { get; } = firstName;

    [Required]
    public string LastName { get; } = lastName;

    public string? RegistrationNumber { get; } = registrationNumber;
}