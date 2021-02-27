namespace Parking.Api.Controllers
{
    using System.Threading.Tasks;
    using Business.Data;
    using Json.Profiles;
    using Microsoft.AspNetCore.Mvc;
    using Model;

    [Route("[controller]")]
    [ApiController]
    public class ProfilesController : ControllerBase
    {
        private readonly IUserRepository userRepository;

        public ProfilesController(IUserRepository userRepository) => this.userRepository = userRepository;

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var user = await this.userRepository.GetUser(this.GetCognitoUserId());

            var response = CreateResponse(user);

            return this.Ok(response);
        }

        [HttpPatch]
        public async Task<IActionResult> PatchAsync([FromBody] ProfilePatchRequest request)
        {
            var existingUser = await this.userRepository.GetUser(this.GetCognitoUserId());

            var updatedUser = new User(
                existingUser.UserId,
                request.AlternativeRegistrationNumber,
                existingUser.CommuteDistance,
                existingUser.EmailAddress,
                existingUser.FirstName,
                existingUser.LastName,
                request.RegistrationNumber);

            await this.userRepository.SaveUser(updatedUser);

            var response = CreateResponse(updatedUser);

            return this.Ok(response);
        }

        private static ProfileResponse CreateResponse(User user)
        {
            var profile = new ProfileData(user.RegistrationNumber, user.AlternativeRegistrationNumber);

            return new ProfileResponse(profile);
        }
    }
}
