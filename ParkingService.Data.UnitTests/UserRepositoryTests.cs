using System.Collections.Generic;
using System.Linq;
using Moq;
using ParkingService.Model;
using Xunit;

namespace ParkingService.Data.UnitTests
{
    public static class UserRepositoryTests
    {
        [Fact]
        public static async void Converts_raw_items_to_users()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            var rawItems = new[]
            {
                new RawItem { PrimaryKey = "USER#Id1", SortKey = "PROFILE", CommuteDistance = 1.23m, EmailAddress = "1@abc.com" },
                new RawItem { PrimaryKey = "USER#Id2", SortKey = "PROFILE", CommuteDistance = 2.34m, EmailAddress = "2@abc.com" },
                new RawItem { PrimaryKey = "USER#Id3", SortKey = "PROFILE", EmailAddress = "3@xyz.co.uk" }
            };
            mockRawItemRepository.Setup(r => r.GetUsers()).ReturnsAsync(rawItems);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetUsers();
            
            Assert.NotNull(result);
            
            Assert.Equal(rawItems.Length, result.Count);

            CheckUser(result, "Id1", 1.23m, "1@abc.com");
            CheckUser(result, "Id2", 2.34m, "2@abc.com");
            CheckUser(result, "Id3", null, "3@xyz.co.uk");
        }

        [Fact]
        public static async void Filters_team_leader_users()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            var rawUsers = new[]
            {
                new RawItem { PrimaryKey = "USER#Id1", SortKey = "PROFILE", CommuteDistance = 1.23m, EmailAddress = "1@abc.com" },
                new RawItem { PrimaryKey = "USER#Id2", SortKey = "PROFILE", CommuteDistance = 2.34m, EmailAddress = "2@abc.com" },
                new RawItem { PrimaryKey = "USER#Id3", SortKey = "PROFILE", EmailAddress = "3@xyz.co.uk" }
            };
            mockRawItemRepository.Setup(r => r.GetUsers()).ReturnsAsync(rawUsers);

            var rawTeamLeaderUserIds = new[] {"Id1", "Id3"};
            mockRawItemRepository.Setup(r => r.GetUserIdsInGroup("TeamLeader")).ReturnsAsync(rawTeamLeaderUserIds);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetTeamLeaderUsers();

            Assert.NotNull(result);

            Assert.Equal(rawTeamLeaderUserIds.Length, result.Count);

            CheckUser(result, "Id1", 1.23m, "1@abc.com");
            CheckUser(result, "Id3", null, "3@xyz.co.uk");
        }

        private static void CheckUser(
            IEnumerable<User> result,
            string expectedUserId,
            decimal? expectedCommuteDistance,
            string expectedEmailAddress)
        {
            var actual = result.Where(u => 
                u.UserId == expectedUserId &&
                u.CommuteDistance == expectedCommuteDistance &&
                u.EmailAddress == expectedEmailAddress);

            Assert.Single(actual);
        }
    }
}