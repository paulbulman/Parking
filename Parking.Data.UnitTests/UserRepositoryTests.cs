// ReSharper disable StringLiteralTypo
namespace Parking.Data.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using Moq;
    using TestHelpers;
    using Xunit;

    public static class UserRepositoryTests
    {
        [Fact]
        public static async Task Create_user_creates_new_identity_provider_user()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var user = CreateUser.With(
                userId: string.Empty, 
                emailAddress: "john.doe@example.com", 
                firstName: "John", 
                lastName: "Doe");
            
            await userRepository.CreateUser(user);

            mockRawItemRepository.Verify(r => r.CreateUser("john.doe@example.com", "John", "Doe"), Times.Once);
        }

        [Fact]
        public static async Task Create_user_creates_new_database_user()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>();
            mockRawItemRepository
                .Setup(r => r.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("User1");

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var user = CreateUser.With(
                userId: string.Empty,
                alternativeRegistrationNumber: "A999XYZ",
                commuteDistance: 1.23m,
                emailAddress: "john.doe@example.com",
                firstName: "John",
                lastName: "Doe",
                registrationNumber: "AB12CDE"
            );

            await userRepository.CreateUser(user);

            mockRawItemRepository.Verify(
                r => r.SaveItem(It.Is<RawItem>(actual =>
                    actual.PrimaryKey == "USER#User1" &&
                    actual.SortKey == "PROFILE" &&
                    actual.AlternativeRegistrationNumber == "A999XYZ" &&
                    actual.CommuteDistance == 1.23m &&
                    actual.EmailAddress == "john.doe@example.com" &&
                    actual.FirstName == "John" &&
                    actual.LastName == "Doe" &&
                    actual.RegistrationNumber == "AB12CDE")),
                Times.Once);
        }

        [Fact]
        public static async Task Create_user_returns_newly_created_user()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>();
            mockRawItemRepository
                .Setup(r => r.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("User1");

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var user = CreateUser.With(
                userId: string.Empty,
                alternativeRegistrationNumber: "A999XYZ",
                commuteDistance: 1.23m,
                emailAddress: "john.doe@example.com",
                firstName: "John",
                lastName: "Doe",
                registrationNumber: "AB12CDE"
            );

            var result = await userRepository.CreateUser(user);

            Assert.NotNull(result);

            CheckUser(new[] { result }, "User1", "A999XYZ", 1.23m, "john.doe@example.com", "John", "Doe", "AB12CDE");
        }

        [Fact]
        public static async Task UserExists_returns_true_when_user_with_given_ID_exists()
        {
            const string UserId = "User1";

            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            mockRawItemRepository.Setup(r => r.GetUser(UserId)).ReturnsAsync(new RawItem());

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.UserExists(UserId);

            Assert.True(result);
        }

        [Fact]
        public static async Task UserExists_returns_false_when_no_user_with_given_ID_exists()
        {
            const string UserId = "User1";

            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            mockRawItemRepository.Setup(r => r.GetUser(UserId)).ReturnsAsync((RawItem)null);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.UserExists(UserId);

            Assert.False(result);
        }

        [Fact]
        public static async Task GetUser_returns_null_when_requested_user_does_not_exist()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            mockRawItemRepository.Setup(r => r.GetUser(It.IsAny<string>())).ReturnsAsync((RawItem)null);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetUser("UserId");

            Assert.Null(result);
        }

        [Fact]
        public static async Task GetUser_converts_raw_item_to_user()
        {
            const string UserId = "User1";

            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            var rawItem = new RawItem
            {
                PrimaryKey = "USER#User1",
                SortKey = "PROFILE",
                AlternativeRegistrationNumber = "W789XYZ",
                CommuteDistance = 1.23m,
                EmailAddress = "1@abc.com",
                FirstName = "Sean",
                LastName = "Cantera",
                RegistrationNumber = "AB12CDE"
            };

            mockRawItemRepository.Setup(r => r.GetUser(UserId)).ReturnsAsync(rawItem);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetUser(UserId);

            Assert.NotNull(result);

            CheckUser(new[] { result }, UserId, "W789XYZ", 1.23m, "1@abc.com", "Sean", "Cantera", "AB12CDE");
        }

        [Fact]
        public static async Task GetUsers_converts_raw_items_to_users()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            var rawItems = new[]
            {
                new RawItem { PrimaryKey = "USER#Id1", SortKey = "PROFILE", AlternativeRegistrationNumber = "W789XYZ", CommuteDistance = 1.23m, EmailAddress = "1@abc.com", FirstName = "Sean", LastName = "Cantera", RegistrationNumber = "AB12CDE"},
                new RawItem { PrimaryKey = "USER#Id2", SortKey = "PROFILE", CommuteDistance = 2.34m, EmailAddress = "2@abc.com", FirstName = "Clyde", LastName = "Memory", RegistrationNumber = "FG34HIJ" },
                new RawItem { PrimaryKey = "USER#Id3", SortKey = "PROFILE", EmailAddress = "3@xyz.co.uk", FirstName = "Kalle", LastName = "Rochewell" }
            };
            mockRawItemRepository.Setup(r => r.GetUsers()).ReturnsAsync(rawItems);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetUsers();

            Assert.NotNull(result);

            Assert.Equal(rawItems.Length, result.Count);

            CheckUser(result, "Id1", "W789XYZ", 1.23m, "1@abc.com", "Sean", "Cantera", "AB12CDE");
            CheckUser(result, "Id2", null, 2.34m, "2@abc.com", "Clyde", "Memory", "FG34HIJ");
            CheckUser(result, "Id3", null, null, "3@xyz.co.uk", "Kalle", "Rochewell", null);
        }

        [Fact]
        public static async Task GetTeamLeaderUsers_filters_team_leader_users()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            var rawUsers = new[]
            {
                new RawItem { PrimaryKey = "USER#Id1", SortKey = "PROFILE", EmailAddress = "1@abc.com", FirstName = "Shalom", LastName = "Georgiades" },
                new RawItem { PrimaryKey = "USER#Id2", SortKey = "PROFILE", EmailAddress = "2@abc.com", FirstName = "Randolf", LastName = "Blogg" },
                new RawItem { PrimaryKey = "USER#Id3", SortKey = "PROFILE", EmailAddress = "3@xyz.co.uk", FirstName = "Kris", LastName = "Whibley" }
            };
            mockRawItemRepository.Setup(r => r.GetUsers()).ReturnsAsync(rawUsers);

            var rawTeamLeaderUserIds = new[] { "Id1", "Id3" };
            mockRawItemRepository.Setup(r => r.GetUserIdsInGroup("TeamLeader")).ReturnsAsync(rawTeamLeaderUserIds);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetTeamLeaderUsers();

            Assert.NotNull(result);

            Assert.Equal(rawTeamLeaderUserIds.Length, result.Count);

            CheckUser(result, "Id1", null, null, "1@abc.com", "Shalom", "Georgiades", null);
            CheckUser(result, "Id3", null, null, "3@xyz.co.uk", "Kris", "Whibley", null);
        }

        [Fact]
        public static async Task Save_user_updates_user_in_identity_provider()
        {
            var user = CreateUser.With(userId: "User1", firstName: "John", lastName: "Doe");

            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            await userRepository.SaveUser(user);

            mockRawItemRepository.Verify(r => r.UpdateUser("User1", "John", "Doe"), Times.Once);

            mockRawItemRepository.Verify(r => r.SaveItem(It.IsAny<RawItem>()), Times.Once);

            mockRawItemRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Save_user_updates_user_in_database()
        {
            var user = CreateUser.With(
                userId: "User1",
                alternativeRegistrationNumber: "A999XYZ",
                commuteDistance: 12.3m,
                emailAddress: "john.doe@example.com",
                firstName: "John",
                lastName: "Doe",
                registrationNumber: "AB12CDE");

            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            await userRepository.SaveUser(user);

            mockRawItemRepository.Verify(
                r => r.SaveItem(It.Is<RawItem>(actual =>
                    actual.PrimaryKey == "USER#User1" &&
                    actual.SortKey == "PROFILE" &&
                    actual.AlternativeRegistrationNumber == "A999XYZ" &&
                    actual.CommuteDistance == 12.3m &&
                    actual.EmailAddress == "john.doe@example.com" &&
                    actual.FirstName == "John" &&
                    actual.LastName == "Doe" &&
                    actual.RegistrationNumber == "AB12CDE")),
                Times.Once);

            mockRawItemRepository.Verify(
                r => r.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

            mockRawItemRepository.VerifyNoOtherCalls();
        }

        private static void CheckUser(
            IEnumerable<User> result,
            string expectedUserId,
            string expectedAlternativeRegistrationNumber,
            decimal? expectedCommuteDistance,
            string expectedEmailAddress,
            string expectedFirstName,
            string expectedLastName,
            string expectedRegistrationNumber)
        {
            var actual = result.Where(u =>
                u.UserId == expectedUserId &&
                u.AlternativeRegistrationNumber == expectedAlternativeRegistrationNumber &&
                u.CommuteDistance == expectedCommuteDistance &&
                u.EmailAddress == expectedEmailAddress &&
                u.FirstName == expectedFirstName &&
                u.LastName == expectedLastName &&
                u.RegistrationNumber == expectedRegistrationNumber);

            Assert.Single(actual);
        }
    }
}