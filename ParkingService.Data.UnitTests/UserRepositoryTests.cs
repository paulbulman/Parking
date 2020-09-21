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
                new RawItem { PrimaryKey = "USER#Id1", SortKey = "PROFILE", CommuteDistance = 1.23m },
                new RawItem { PrimaryKey = "USER#Id2", SortKey = "PROFILE", CommuteDistance = 2.34m }
            };
            mockRawItemRepository.Setup(r => r.GetUsers()).ReturnsAsync(rawItems);

            var userRepository = new UserRepository(mockRawItemRepository.Object);

            var result = await userRepository.GetUsers();
            
            Assert.NotNull(result);
            
            Assert.Equal(2, result.Count);

            CheckUser(result, "Id1", 1.23m);
            CheckUser(result, "Id2", 2.34m);
        }

        private static void CheckUser(
            IEnumerable<User> result,
            string expectedUserId,
            decimal expectedCommuteDistance)
        {
            var actual = result.Where(u => 
                u.UserId == expectedUserId && 
                u.CommuteDistance == expectedCommuteDistance);

            Assert.Single(actual);
        }
    }
}