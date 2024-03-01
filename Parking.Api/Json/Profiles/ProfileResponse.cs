namespace Parking.Api.Json.Profiles;

using System.ComponentModel.DataAnnotations;

public class ProfileResponse(ProfileData profile)
{
    [Required]
    public ProfileData Profile { get; } = profile;
}