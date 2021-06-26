namespace Parking.Api.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Json.Users;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
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
        [ProducesResponseType(typeof(MultipleUsersResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            var users = await this.userRepository.GetUsers();

            var usersData = users.OrderForDisplay().Select(CreateUsersData);

            var response = new MultipleUsersResponse(usersData);

            return this.Ok(response);
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(SingleUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(string userId)
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

        [HttpPost]
        [ProducesResponseType(typeof(SingleUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostAsync([FromBody] UserPostRequest request)
        {
            var newUser = new User(
                string.Empty,
                request.AlternativeRegistrationNumber,
                request.CommuteDistance,
                request.EmailAddress,
                request.FirstName,
                request.LastName,
                request.RegistrationNumber);

            var user = await this.userRepository.CreateUser(newUser);

            var usersData = CreateUsersData(user);

            var response = new SingleUserResponse(usersData);

            return this.Ok(response);
        }

        [HttpPatch("{userId}")]
        [ProducesResponseType(typeof(SingleUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchAsync(string userId, [FromBody] UserPatchRequest request)
        {
            var existingUser = await this.userRepository.GetUser(userId);

            if (existingUser == null)
            {
                return this.NotFound();
            }

            var updatedUser = new User(
                existingUser.UserId,
                request.AlternativeRegistrationNumber,
                request.CommuteDistance,
                existingUser.EmailAddress,
                request.FirstName,
                request.LastName,
                request.RegistrationNumber);

            await this.userRepository.SaveUser(updatedUser);

            var usersData = CreateUsersData(updatedUser);

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