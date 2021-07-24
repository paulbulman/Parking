namespace Parking.Data.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class RequestRepositoryTests
    {
        [Fact]
        public static async Task GetRequests_returns_empty_collection_when_no_matching_raw_item_exists()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            SetupMockRepository(mockDatabaseProvider, new YearMonth(2020, 8));
            SetupMockRepository(mockDatabaseProvider, new YearMonth(2020, 9));

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);

            var result = await requestRepository.GetRequests(1.August(2020), 30.September(2020));

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("A", RequestStatus.Allocated)]
        [InlineData("C", RequestStatus.Cancelled)]
        [InlineData("H", RequestStatus.HardInterrupted)]
        [InlineData("I", RequestStatus.Interrupted)]
        [InlineData("P", RequestStatus.Pending)]
        [InlineData("S", RequestStatus.SoftInterrupted)]
        public static async Task GetRequests_converts_raw_string_value_to_status_enum(
            string rawValue,
            RequestStatus expectedRequestStatus)
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            SetupMockRepository(
                mockDatabaseProvider,
                new YearMonth(2020, 9),
                CreateRawItem("User1", "2020-09", KeyValuePair.Create("30", rawValue)));

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);

            var result = await requestRepository.GetRequests(30.September(2020), 30.September(2020));

            CheckRequest(result, "User1", 30.September(2020), expectedRequestStatus);
        }

        [Fact]
        public static async Task GetRequests_converts_raw_items_for_multiple_users_to_requests()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            SetupMockRepository(
                mockDatabaseProvider,
                new YearMonth(2020, 8),
                CreateRawItem(
                    "User1",
                    "2020-08",
                    KeyValuePair.Create("02", "I"),
                    KeyValuePair.Create("13", "A")),
                CreateRawItem(
                    "User2",
                    "2020-08",
                    KeyValuePair.Create("02", "C")));
            SetupMockRepository(
                mockDatabaseProvider,
                new YearMonth(2020, 9),
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("30", "A")));

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);

            var result = await requestRepository.GetRequests(1.August(2020), 30.September(2020));

            Assert.NotNull(result);

            Assert.Equal(4, result.Count);

            CheckRequest(result, "User1", 2.August(2020), RequestStatus.Interrupted);
            CheckRequest(result, "User1", 13.August(2020), RequestStatus.Allocated);
            CheckRequest(result, "User2", 2.August(2020), RequestStatus.Cancelled);
            CheckRequest(result, "User1", 30.September(2020), RequestStatus.Allocated);
        }

        [Fact]
        public static async Task GetRequests_converts_raw_items_for_single_user_to_requests()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            SetupMockRepository(
                mockDatabaseProvider,
                "User1",
                new YearMonth(2020, 8),
                CreateRawItem(
                    "User1",
                    "2020-08",
                    KeyValuePair.Create("02", "I"),
                    KeyValuePair.Create("13", "A")));
            SetupMockRepository(
                mockDatabaseProvider,
                "User1",
                new YearMonth(2020, 9),
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("30", "C")));

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);

            var result = await requestRepository.GetRequests("User1", 1.August(2020), 30.September(2020));

            Assert.NotNull(result);

            Assert.Equal(3, result.Count);

            CheckRequest(result, "User1", 2.August(2020), RequestStatus.Interrupted);
            CheckRequest(result, "User1", 13.August(2020), RequestStatus.Allocated);
            CheckRequest(result, "User1", 30.September(2020), RequestStatus.Cancelled);
        }

        [Fact]
        public static async Task GetRequests_filters_requests_outside_specified_date_range()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            SetupMockRepository(
                mockDatabaseProvider,
                new YearMonth(2020, 8),
                CreateRawItem("User1", "2020-08", KeyValuePair.Create("02", "I")),
                CreateRawItem("User2", "2020-08", KeyValuePair.Create("02", "C")));

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);

            var result = await requestRepository.GetRequests(3.August(2020), 31.August(2020));

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public static async Task SaveRequests_handles_empty_list()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>();

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);

            await requestRepository.SaveRequests(new List<Request>());

            mockDatabaseProvider.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(RequestStatus.Allocated, "A")]
        [InlineData(RequestStatus.Cancelled, "C")]
        [InlineData(RequestStatus.HardInterrupted, "H")]
        [InlineData(RequestStatus.Interrupted, "I")]
        [InlineData(RequestStatus.Pending, "P")]
        [InlineData(RequestStatus.SoftInterrupted, "S")]
        public static async Task SaveRequests_converts_status_enum_to_raw_string_value(
            RequestStatus requestStatus,
            string expectedRawValue)
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);
            mockDatabaseProvider
                .Setup(p => p.GetRequests("User1", new YearMonth(2020, 9)))
                .ReturnsAsync(new List<RawItem>());
            mockDatabaseProvider
                .Setup(p => p.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);
            
            var requests = new[] { new Request("User1", 1.September(2020), requestStatus) };
            
            await requestRepository.SaveRequests(requests);

            var expectedRawItems = new[]
            {
                CreateRawItem("User1", "2020-09", KeyValuePair.Create("01", expectedRawValue)),
            };

            mockDatabaseProvider.Verify(p => p.SaveItems(
                    It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }

        [Fact]
        public static async Task SaveRequests_converts_requests_to_raw_items()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);
            mockDatabaseProvider
                .Setup(p => p.GetRequests(It.IsAny<YearMonth>()))
                .ReturnsAsync(new List<RawItem>());
            mockDatabaseProvider
                .Setup(p => p.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);
            await requestRepository.SaveRequests(
                new[]
                {
                    new Request("User1", 1.September(2020), RequestStatus.Allocated),
                    new Request("User1", 2.September(2020), RequestStatus.Cancelled),
                    new Request("User1", 3.October(2020), RequestStatus.Interrupted),
                    new Request("User2", 4.October(2020), RequestStatus.Interrupted)
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("01", "A"),
                    KeyValuePair.Create("02", "C")),
                CreateRawItem(
                    "User1",
                    "2020-10",
                    KeyValuePair.Create("03", "I")),
                CreateRawItem(
                    "User2",
                    "2020-10",
                    KeyValuePair.Create("04", "I")),
            };

            mockDatabaseProvider.Verify(p => p.SaveItems(
                It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }

        [Fact]
        public static async Task SaveRequests_combines_new_and_existing_requests()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);
            mockDatabaseProvider
                .Setup(p => p.GetRequests(new YearMonth(2020, 9)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem(
                        "User1",
                        "2020-09",
                        KeyValuePair.Create("01", "A"),
                        KeyValuePair.Create("02", "I")),
                    CreateRawItem(
                        "User2",
                        "2020-09",
                        KeyValuePair.Create("03", "A"))
                });
            mockDatabaseProvider
                .Setup(p => p.GetRequests(new YearMonth(2020, 10)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem(
                        "User1",
                        "2020-10",
                        KeyValuePair.Create("03", "I"))
                });
            mockDatabaseProvider
                .Setup(p => p.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);
            await requestRepository.SaveRequests(
                new[]
                {
                    new Request("User1", 2.September(2020), RequestStatus.Allocated),
                    new Request("User1", 3.September(2020), RequestStatus.Interrupted),
                    new Request("User2", 3.October(2020), RequestStatus.Cancelled),
                    new Request("User2", 4.October(2020), RequestStatus.Interrupted)
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("01", "A"),
                    KeyValuePair.Create("02", "A"),
                    KeyValuePair.Create("03", "I")),
                CreateRawItem(
                    "User2",
                    "2020-09",
                    KeyValuePair.Create("03", "A")),
                CreateRawItem(
                    "User1",
                    "2020-10",
                    KeyValuePair.Create("03", "I")),
                CreateRawItem(
                    "User2",
                    "2020-10",
                    KeyValuePair.Create("03", "C"),
                    KeyValuePair.Create("04", "I")),

            };

            mockDatabaseProvider.Verify(
                p => p.SaveItems(It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }

        [Fact]
        public static async Task SaveRequests_fetches_requests_by_user_when_all_updated_requests_are_for_same_user()
        {
            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);
            mockDatabaseProvider
                .Setup(p => p.GetRequests("User1", new YearMonth(2020, 9)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem(
                        "User1",
                        "2020-09",
                        KeyValuePair.Create("01", "A"),
                        KeyValuePair.Create("02", "I")),
                });
            mockDatabaseProvider
                .Setup(p => p.GetRequests("User1", new YearMonth(2020, 10)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem(
                        "User1",
                        "2020-10",
                        KeyValuePair.Create("03", "I"))
                });
            mockDatabaseProvider
                .Setup(p => p.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var requestRepository = new RequestRepository(mockDatabaseProvider.Object);
            await requestRepository.SaveRequests(
                new[]
                {
                    new Request("User1", 2.September(2020), RequestStatus.Allocated),
                    new Request("User1", 3.September(2020), RequestStatus.Interrupted),
                    new Request("User1", 3.October(2020), RequestStatus.Cancelled),
                    new Request("User1", 4.October(2020), RequestStatus.Interrupted)
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "User1",
                    "2020-09",
                    KeyValuePair.Create("01", "A"),
                    KeyValuePair.Create("02", "A"),
                    KeyValuePair.Create("03", "I")),
                CreateRawItem(
                    "User1",
                    "2020-10",
                    KeyValuePair.Create("03", "C"),
                    KeyValuePair.Create("04", "I")),
            };

            mockDatabaseProvider.Verify(
                p => p.SaveItems(It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }

        private static void SetupMockRepository(
            Mock<IDatabaseProvider> mockDatabaseProvider,
            YearMonth yearMonth,
            params RawItem[] mockResult) =>
            mockDatabaseProvider
                .Setup(p => p.GetRequests(yearMonth))
                .Returns(Task.FromResult((IReadOnlyCollection<RawItem>)mockResult));

        private static void SetupMockRepository(
            Mock<IDatabaseProvider> mockDatabaseProvider,
            string userId,
            YearMonth yearMonth,
            params RawItem[] mockResult) =>
            mockDatabaseProvider
                .Setup(p => p.GetRequests(userId, yearMonth))
                .Returns(Task.FromResult((IReadOnlyCollection<RawItem>)mockResult));

        private static RawItem CreateRawItem(
            string userId,
            string monthKey,
            params KeyValuePair<string, string>[] requestData) =>
            RawItem.CreateRequests(
                primaryKey: $"USER#{userId}",
                sortKey: $"REQUESTS#{monthKey}",
                requests: new Dictionary<string, string>(requestData));

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
            public bool Equals(RawItem? first, RawItem? second) =>
                first != null &&
                second != null &&
                first.PrimaryKey == second.PrimaryKey &&
                first.SortKey == second.SortKey &&
                CompareRequests(first.Requests, second.Requests);

            private static bool CompareRequests(IDictionary<string, string>? first, IDictionary<string, string>? second) =>
                first != null &&
                second != null &&
                first.Keys.Count == second.Keys.Count &&
                first.Keys.ToList().All(key => second.ContainsKey(key) && second[key] == first[key]);

            public int GetHashCode(RawItem rawItem) => HashCode.Combine(rawItem.PrimaryKey, rawItem.SortKey, rawItem.Requests);
        }
    }
}
