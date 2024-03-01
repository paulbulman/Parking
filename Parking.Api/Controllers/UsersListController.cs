namespace Parking.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Business.Data;
using Json.UsersList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

[Authorize(Policy = "IsTeamLeader")]
[Route("[controller]")]
[ApiController]
public class UsersListController : ControllerBase
{
    private readonly IUserRepository userRepository;

    public UsersListController(IUserRepository userRepository) => this.userRepository = userRepository;

    [HttpGet]
    [ProducesResponseType(typeof(UsersListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var users = await this.userRepository.GetUsers();

        var userUsers = users
            .OrderForDisplay()
            .Select(u => new UsersListUser(u.UserId, u.DisplayName()));

        var response = new UsersListResponse(userUsers);

        return this.Ok(response);
    }
}