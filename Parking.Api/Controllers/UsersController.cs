namespace Parking.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Json.Users;
    using Microsoft.AspNetCore.Authorization;

    [Authorize(Policy = "IsTeamLeader")]
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository userRepository;

        public UsersController(IUserRepository userRepository) => this.userRepository = userRepository;

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var users = await this.userRepository.GetUsers();

            var userUsers = users
                .OrderBy(u => u.LastName)
                .Select(u => new UsersUser(u.UserId, u.DisplayName()));

            var response = new UsersResponse(userUsers);

            return this.Ok(response);
        }
    }
}
