namespace Parking.Data.UnitTests
{
    using System;
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
        public static async void GetRequests_returns_empty_collection_when_no_matching_raw_item_exists()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(mockRawItemRepository, new YearMonth(2020, 8));
            SetupMockRepository(mockRawItemRepository, new YearMonth(2020, 9));

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);

            var result = await requestRepository.GetRequests(1.August(2020), 30.September(2020));

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public static async void GetRequests_converts_raw_items_for_multiple_users_to_requests()
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
        public static async void GetRequests_converts_raw_items_for_single_user_to_requests()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(
                mockRawItemRepository,
                "User1",
                new YearMonth(2020, 8),
                CreateRawItem(
                    "User1",
                    "2020-08",
                    KeyValuePair.Create("02", "REQUESTED"),
                    KeyValuePair.Create("13", "ALLOCATED")));
            SetupMockRepository(
                mockRawItemRepository,
                "User1",
                new YearMonth(2020, 9),
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("30", "CANCELLED")));

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);

            var result = await requestRepository.GetRequests("User1", 1.August(2020), 30.September(2020));

            Assert.NotNull(result);

            Assert.Equal(3, result.Count);

            CheckRequest(result, "User1", 2.August(2020), RequestStatus.Requested);
            CheckRequest(result, "User1", 13.August(2020), RequestStatus.Allocated);
            CheckRequest(result, "User1", 30.September(2020), RequestStatus.Cancelled);
        }

        [Fact]
        public static async void GetRequests_filters_requests_outside_specified_date_range()
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

        [Fact]
        public static async void SaveRequests_handles_empty_list()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);

            await requestRepository.SaveRequests(new List<Request>());

            mockRawItemRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async void SaveRequests_converts_requests_to_raw_items()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);
            mockRawItemRepository
                .Setup(r => r.GetRequests(It.IsAny<YearMonth>()))
                .ReturnsAsync(new List<RawItem>());
            mockRawItemRepository
                .Setup(r => r.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);
            await requestRepository.SaveRequests(
                new[]
                {
                    new Request("User1", 1.September(2020), RequestStatus.Allocated),
                    new Request("User1", 2.September(2020), RequestStatus.Cancelled),
                    new Request("User1", 3.October(2020), RequestStatus.Requested),
                    new Request("User2", 4.October(2020), RequestStatus.Requested)
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("01", "ALLOCATED"),
                    KeyValuePair.Create("02", "CANCELLED")),
                CreateRawItem(
                    "User1",
                    "2020-10",
                    KeyValuePair.Create("03", "REQUESTED")),
                CreateRawItem(
                    "User2",
                    "2020-10",
                    KeyValuePair.Create("04", "REQUESTED")),
            };

            mockRawItemRepository.Verify(r => r.SaveItems(
                It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }
        
        [Fact]
        public static async void SaveRequests_combines_new_and_existing_requests()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);
            mockRawItemRepository
                .Setup(r => r.GetRequests(new YearMonth(2020, 9)))
                .ReturnsAsync(new []
                {
                    CreateRawItem(
                        "User1", 
                        "2020-09", 
                        KeyValuePair.Create("01", "ALLOCATED"),
                        KeyValuePair.Create("02", "REQUESTED")),
                    CreateRawItem(
                        "User2",
                        "2020-09",
                        KeyValuePair.Create("03", "ALLOCATED"))
                });
            mockRawItemRepository
                .Setup(r => r.GetRequests(new YearMonth(2020, 10)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem(
                        "User1",
                        "2020-10",
                        KeyValuePair.Create("03", "REQUESTED"))
                });
            mockRawItemRepository
                .Setup(r => r.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var requestRepository = new RequestRepository(mockRawItemRepository.Object);
            await requestRepository.SaveRequests(
                new[]
                {
                    new Request("User1", 2.September(2020), RequestStatus.Allocated),
                    new Request("User1", 3.September(2020), RequestStatus.Requested),
                    new Request("User2", 3.October(2020), RequestStatus.Cancelled),
                    new Request("User2", 4.October(2020), RequestStatus.Requested)
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("01", "ALLOCATED"),
                    KeyValuePair.Create("02", "ALLOCATED"),
                    KeyValuePair.Create("03", "REQUESTED")),
                CreateRawItem(
                    "User2",
                    "2020-09",
                    KeyValuePair.Create("03", "ALLOCATED")),
                CreateRawItem(
                    "User1",
                    "2020-10",
                    KeyValuePair.Create("03", "REQUESTED")),
                CreateRawItem(
                    "User2",
                    "2020-10",
                    KeyValuePair.Create("03", "CANCELLED"),
                    KeyValuePair.Create("04", "REQUESTED")),
                
            };

            mockRawItemRepository.Verify(r => r.SaveItems(
                    It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }

        private static void SetupMockRepository(
            Mock<IRawItemRepository> mockRawItemRepository,
            YearMonth yearMonth,
            params RawItem[] mockResult) =>
            mockRawItemRepository
                .Setup(r => r.GetRequests(yearMonth))
                .Returns(Task.FromResult((IReadOnlyCollection<RawItem>)mockResult));

        private static void SetupMockRepository(
            Mock<IRawItemRepository> mockRawItemRepository,
            string userId,
            YearMonth yearMonth,
            params RawItem[] mockResult) =>
            mockRawItemRepository
                .Setup(r => r.GetRequests(userId, yearMonth))
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

        private static bool CheckRawItems(
            IReadOnlyCollection<RawItem> expected,
            IReadOnlyCollection<RawItem> actual) =>
            actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RawRequestsComparer()));

        private class RawRequestsComparer : IEqualityComparer<RawItem>
        {
            public bool Equals(RawItem first, RawItem second) =>
                first != null &&
                second != null &&
                first.PrimaryKey == second.PrimaryKey &&
                first.SortKey == second.SortKey &&
                CompareRequests(first.Requests, second.Requests);

            private static bool CompareRequests(IDictionary<string, string> first, IDictionary<string, string> second) =>
                first != null &&
                second != null &&
                first.Keys.Count == second.Keys.Count &&
                first.Keys.ToList().All(key => second.ContainsKey(key) && second[key] == first[key]);

            public int GetHashCode(RawItem rawItem) => HashCode.Combine(rawItem.PrimaryKey, rawItem.SortKey, rawItem.Requests);
        }
    }
}
