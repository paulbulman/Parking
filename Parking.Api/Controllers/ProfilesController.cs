namespace Parking.Api.Controllers
{
    using System.Threading.Tasks;
    using Business.Data;
    using Json.Profiles;
    using Microsoft.AspNetCore.Mvc;

    [Route("[controller]")]
    [ApiController]
    public class ProfilesController : ControllerBase
    {
        private readonly IUserRepository userRepository;

        public ProfilesController(IUserRepository userRepository) => this.userRepository = userRepository;

        public async Task<IActionResult> GetAsync()
        {
            var user = await this.userRepository.GetUser(this.GetCognitoUserId());

            var profile = new ProfileData(user.RegistrationNumber, user.AlternativeRegistrationNumber);

            var response = new ProfileResponse(profile);

            return this.Ok(response);
        }
    }
}
