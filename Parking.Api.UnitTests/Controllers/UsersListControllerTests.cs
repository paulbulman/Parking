// ReSharper disable StringLiteralTypo
namespace Parking.Api.UnitTests.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.UsersList;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;

    public static class UsersListControllerTests
    {
        [Fact]
        public static async Task Returns_sorted_list_of_users()
        {
            var users = new[]
            {
                CreateUser.With(userId: "User1", firstName: "Silvester", lastName: "Probet"),
                CreateUser.With(userId: "User2", firstName: "Kendricks", lastName: "Hawke"),
                CreateUser.With(userId: "User3", firstName: "Rupert", lastName: "Trollope"),
            };

            var controller = new UsersListController(CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<UsersListResponse>(result);

            Assert.NotNull(resultValue.Users);

            var actualUsers = resultValue.Users.ToArray();

            Assert.Equal(3, actualUsers.Length);

            Assert.Equal("User2", actualUsers[0].UserId);
            Assert.Equal("Kendricks Hawke", actualUsers[0].Name);

            Assert.Equal("User1", actualUsers[1].UserId);
            Assert.Equal("Silvester Probet", actualUsers[1].Name);

            Assert.Equal("User3", actualUsers[2].UserId);
            Assert.Equal("Rupert Trollope", actualUsers[2].Name);
        }
    }
}