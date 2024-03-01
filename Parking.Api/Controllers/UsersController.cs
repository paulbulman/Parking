namespace Parking.Api.Controllers;

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
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MultipleUsersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var users = await userRepository.GetUsers();

        var usersData = users.OrderForDisplay().Select(CreateUsersData);

        var response = new MultipleUsersResponse(usersData);

        return this.Ok(response);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(SingleUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(string userId)
    {
        var user = await userRepository.GetUser(userId);

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
            userId: string.Empty,
            alternativeRegistrationNumber: request.AlternativeRegistrationNumber,
            commuteDistance: request.CommuteDistance,
            emailAddress: request.EmailAddress,
            firstName: request.FirstName,
            lastName: request.LastName,
            registrationNumber: request.RegistrationNumber,
            requestReminderEnabled: true,
            reservationReminderEnabled: true);

        var user = await userRepository.CreateUser(newUser);

        var usersData = CreateUsersData(user);

        var response = new SingleUserResponse(usersData);

        return this.Ok(response);
    }

    [HttpPatch("{userId}")]
    [ProducesResponseType(typeof(SingleUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAsync(string userId, [FromBody] UserPatchRequest request)
    {
        var existingUser = await userRepository.GetUser(userId);

        if (existingUser == null)
        {
            return this.NotFound();
        }

        var updatedUser = new User(
            userId: existingUser.UserId,
            alternativeRegistrationNumber: request.AlternativeRegistrationNumber,
            commuteDistance: request.CommuteDistance,
            emailAddress: existingUser.EmailAddress,
            firstName: request.FirstName,
            lastName: request.LastName,
            registrationNumber: request.RegistrationNumber,
            requestReminderEnabled: existingUser.RequestReminderEnabled,
            reservationReminderEnabled: existingUser.ReservationReminderEnabled);

        await userRepository.UpdateUser(updatedUser);

        var usersData = CreateUsersData(updatedUser);

        var response = new SingleUserResponse(usersData);

        return this.Ok(response);
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string userId)
    {
        var existingUser = await userRepository.GetUser(userId);

        if (existingUser == null)
        {
            return this.NotFound();
        }

        await userRepository.DeleteUser(existingUser);

        return this.NoContent();
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