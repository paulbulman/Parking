namespace Parking.Api.UnitTests.Controllers
{
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Profiles;
    using Business.Data;
    using Model;
    using Moq;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;

    public static class ProfilesControllerTests
    {
        [Fact]
        public static async Task Returns_profile_for_current_user()
        {
            var user = CreateUser.With(userId: "User1", registrationNumber: "AB123CDE", alternativeRegistrationNumber: "A999XYZ");

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

        [Fact]
        public static async Task Saves_combined_updated_editable_properties_and_existing_readonly_properties()
        {
            const string UserId = "User1";

            var existingUser = CreateUser.With(
                userId: UserId,
                alternativeRegistrationNumber: "Z999ABC",
                commuteDistance: 12.3m,
                emailAddress: "john.doe@example.com",
                firstName: "John",
                lastName: "Doe",
                registrationNumber: "AB12CDE");

            var mockUserRepository = new Mock<IUserRepository>();

            mockUserRepository
                .Setup(r => r.GetUser(UserId))
                .ReturnsAsync(existingUser);

            var request = new ProfilePatchRequest("__NEW_REG__", "__NEW_ALTERNATIVE_REG__");

            var controller = new ProfilesController(mockUserRepository.Object)
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            await controller.PatchAsync(request);

            mockUserRepository.Verify(r => r.GetUser(UserId), Times.Once);

            mockUserRepository.Verify(
                r => r.SaveUser(
                    It.Is<User>(u => 
                        u.UserId == UserId &&
                        u.AlternativeRegistrationNumber == "__NEW_ALTERNATIVE_REG__" &&
                        u.CommuteDistance == 12.3m &&
                        u.EmailAddress == "john.doe@example.com" &&
                        u.FirstName == "John" &&
                        u.LastName == "Doe" &&
                        u.RegistrationNumber == "__NEW_REG__")),
                Times.Once);

            mockUserRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Returns_updated_profile_after_saving()
        {
            const string UserId = "User1";

            var existingUser = CreateUser.With(userId: UserId);

            var mockUserRepository = new Mock<IUserRepository>();

            mockUserRepository
                .Setup(r => r.GetUser(UserId))
                .ReturnsAsync(existingUser);

            var request = new ProfilePatchRequest("__NEW_REG__", "__NEW_ALTERNATIVE_REG__");

            var controller = new ProfilesController(mockUserRepository.Object)
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            var result = await controller.PatchAsync(request);

            var resultValue = GetResultValue<ProfileResponse>(result);

            Assert.NotNull(resultValue.Profile);

            Assert.Equal("__NEW_REG__", resultValue.Profile.RegistrationNumber);
            Assert.Equal("__NEW_ALTERNATIVE_REG__", resultValue.Profile.AlternativeRegistrationNumber);
        }
    }
}