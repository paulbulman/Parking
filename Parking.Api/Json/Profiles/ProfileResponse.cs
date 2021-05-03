namespace Parking.Api.Json.Profiles
{
    using System.ComponentModel.DataAnnotations;

    public class ProfileResponse
    {
        public ProfileResponse(ProfileData profile) => this.Profile = profile;

        [Required]
        public ProfileData Profile { get; }
    }
}