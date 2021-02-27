namespace Parking.Api.UnitTests.Controllers
{
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Profiles;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;

    public static class ProfilesControllerTests
    {
        [Fact]
        public static async Task Returns_profile_for_current_user()
        {
            var user = CreateUser.With(userId: "User1", registrationNumber: "AB123CDE", alternativeRegistrationNumber: "A999XYZ");

            CreateUserRepository.WithUser("User1", user);

            var controller = new ProfilesController(CreateUserRepository.WithUser("User1", user))
            {
                ControllerContext = CreateControllerContext.WithUsername("User1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<ProfileResponse>(result);

            Assert.NotNull(resultValue.Profile);

            Assert.Equal("AB123CDE", resultValue.Profile.RegistrationNumber);
            Assert.Equal("A999XYZ", resultValue.Profile.AlternativeRegistrationNumber);
        }
    }
}