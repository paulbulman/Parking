namespace Parking.Api.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Json.Users;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Model;

    [Authorize(Policy = "IsUserAdmin")]
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

            var usersData = users.OrderForDisplay().Select(CreateUsersData);

            var response = new MultipleUsersResponse(usersData);

            return this.Ok(response);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAsync(string userId)
        {
            var user = await this.userRepository.GetUser(userId);

            if (user == null)
            {
                return this.NotFound();
            }

            var usersData = CreateUsersData(user);

            var response = new SingleUserResponse(usersData);

            return this.Ok(response);
        }

        private static UsersData CreateUsersData(User user) =>
            new UsersData(
                userId: user.UserId,
                alternativeRegistrationNumber: user.AlternativeRegistrationNumber,
                commuteDistance: user.CommuteDistance,
                firstName: user.FirstName,
                lastName: user.LastName,
                registrationNumber: user.RegistrationNumber);
    }
}