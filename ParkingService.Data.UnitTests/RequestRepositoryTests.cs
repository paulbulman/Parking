namespace ParkingService.Data.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class RequestRepositoryTests
    {
        [Fact]
        public static async void Converts_raw_items_to_requests()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(
                mockRawItemRepository,
                new YearMonth(2020, 8),
                CreateRawItem(
                    "User1",
                    "2020-08",
                    KeyValuePair.Create("02", "REQUESTED"),
                    KeyValuePair.Create("13", "ALLOCATED")),
                CreateRawItem(
                    "User2",
                    "2020-08",
                    KeyValuePair.Create("02", "CANCELLED")));
            SetupMockRepository(
                mockRawItemRepository,
                new YearMonth(2020, 9),
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("30", "ALLOCATED")));

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);

            var result = await requestRepository.GetRequests(1.August(2020), 30.September(2020));

            Assert.NotNull(result);

            Assert.Equal(4, result.Count);

            CheckRequest(result, "User1", 2.August(2020), RequestStatus.Requested);
            CheckRequest(result, "User1", 13.August(2020), RequestStatus.Allocated);
            CheckRequest(result, "User2", 2.August(2020), RequestStatus.Cancelled);
            CheckRequest(result, "User1", 30.September(2020), RequestStatus.Allocated);
        }

        [Fact]
        public static async void Filters_requests_outside_specified_date_range()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(
                mockRawItemRepository,
                new YearMonth(2020, 8),
                CreateRawItem("User1", "2020-08", KeyValuePair.Create("02", "REQUESTED")),
                CreateRawItem("User2", "2020-08", KeyValuePair.Create("02", "CANCELLED")));

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);

            var result = await requestRepository.GetRequests(3.August(2020), 31.August(2020));

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        private static void SetupMockRepository(
            Mock<IRawItemRepository> mockRawItemRepository,
            YearMonth yearMonth,
            params RawItem[] mockResult) =>
            mockRawItemRepository
                .Setup(r => r.GetRequests(yearMonth))
                .Returns(Task.FromResult((IReadOnlyCollection<RawItem>)mockResult));

        private static RawItem CreateRawItem(
            string userId,
            string monthKey,
            params KeyValuePair<string, string>[] requestData) =>
            new RawItem
            {
                PrimaryKey = $"USER#{userId}",
                SortKey = $"REQUESTS#{monthKey}",
                Requests = new Dictionary<string, string>(requestData)
            };

        private static void CheckRequest(
            IEnumerable<Request> result,
            string expectedUserId,
            LocalDate expectedDate,
            RequestStatus expectedStatus)
        {
            var actual = result.Where(r =>
                r.UserId == expectedUserId &&
                r.Date == expectedDate &&
                r.Status == expectedStatus);

            Assert.Single(actual);
        }
    }
}
