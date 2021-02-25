namespace Parking.Api.Json.Profiles
{
    public class ProfileResponse
    {
        public ProfileResponse(ProfileData profile) => this.Profile = profile;

        public ProfileData Profile { get; }
    }
}