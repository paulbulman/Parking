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

    public static class ReservationRepositoryTests
    {
        [Fact]
        public static async Task GetReservations_returns_empty_collection_when_no_matching_raw_item_exists()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(mockRawItemRepository, new YearMonth(2020, 8));
            SetupMockRepository(mockRawItemRepository, new YearMonth(2020, 9));

            var reservationRepository = new ReservationRepository(mockRawItemRepository.Object);

            var result = await reservationRepository.GetReservations(1.August(2020), 30.September(2020));

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public static async Task GetReservations_converts_raw_items_to_reservations()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(
                mockRawItemRepository,
                new YearMonth(2020, 8),
                CreateRawItem(
                    "2020-08",
                    KeyValuePair.Create("02", new List<string> {"User1", "User2"}),
                    KeyValuePair.Create("13", new List<string> {"User1"})));
            SetupMockRepository(
                mockRawItemRepository,
                new YearMonth(2020, 9),
                CreateRawItem(
                    "2020-09",KeyValuePair.Create("02", new List<string> { "User1" })));

            var reservationRepository = new ReservationRepository(mockRawItemRepository.Object);

            var result = await reservationRepository.GetReservations(1.August(2020), 30.September(2020));

            Assert.NotNull(result);

            Assert.Equal(4, result.Count);

            CheckReservation(result, "User1", 2.August(2020));
            CheckReservation(result, "User2", 2.August(2020));
            CheckReservation(result, "User1", 13.August(2020));
            CheckReservation(result, "User1", 2.September(2020));
        }

        [Fact]
        public static async Task GetReservations_filters_reservations_outside_specified_date_range()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            SetupMockRepository(
                mockRawItemRepository,
                new YearMonth(2020, 8),
                CreateRawItem("2020-08", KeyValuePair.Create("02", new List<string> { "User1", "User2" })));

            var reservationRepository = new ReservationRepository(mockRawItemRepository.Object);

            var result = await reservationRepository.GetReservations(3.August(2020), 31.August(2020));

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public static async Task SaveReservations_handles_empty_list()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var reservationRepository = new ReservationRepository(mockRawItemRepository.Object);

            await reservationRepository.SaveReservations(new List<Reservation>());

            mockRawItemRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task SaveReservations_converts_reservations_to_raw_items()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);
            mockRawItemRepository
                .Setup(r => r.GetReservations(It.IsAny<YearMonth>()))
                .ReturnsAsync(new List<RawItem>());
            mockRawItemRepository
                .Setup(r => r.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var reservationRepository = new ReservationRepository(mockRawItemRepository.Object);
            await reservationRepository.SaveReservations(
                new[]
                {
                    new Reservation("User1", 1.March(2021)),
                    new Reservation("User1", 2.March(2021)),
                    new Reservation("User1", 1.April(2021)),
                    new Reservation("User2", 1.April(2021)),
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "2021-03",
                    KeyValuePair.Create("01", new List<string> {"User1"}),
                    KeyValuePair.Create("02", new List<string> {"User1"})),
                CreateRawItem(
                    "2021-04",
                    KeyValuePair.Create("01", new List<string> {"User1", "User2"}))
            };

            mockRawItemRepository.Verify(r => r.SaveItems(
                    It.Is<IEnumerable<RawItem>>(actual => CheckRawItems(expectedRawItems, actual.ToList()))),
                Times.Once);
        }

        [Fact]
        public static async Task SaveReservations_combines_new_and_existing_reservations()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);
            mockRawItemRepository
                .Setup(r => r.GetReservations(new YearMonth(2021, 3)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem("2021-03", KeyValuePair.Create("01", new List<string>{"User1", "User2"})),
                    CreateRawItem("2021-03", KeyValuePair.Create("02", new List<string>{"User1"}))
                });
            mockRawItemRepository
                .Setup(r => r.GetReservations(new YearMonth(2021, 4)))
                .ReturnsAsync(new[]
                {
                    CreateRawItem("2021-04", KeyValuePair.Create("03", new List<string>{"User1", "User2"}))
                });
            mockRawItemRepository
                .Setup(r => r.SaveItems(It.IsAny<IEnumerable<RawItem>>()))
                .Returns(Task.CompletedTask);

            var reservationRepository = new ReservationRepository(mockRawItemRepository.Object);
            await reservationRepository.SaveReservations(
                new[]
                {
                    new Reservation("User2", 1.March(2021)),
                    new Reservation("User4", 1.March(2021)),
                    new Reservation("User1", 3.March(2021)),
                    new Reservation("User3", 3.April(2021)),
                });

            var expectedRawItems = new[]
            {
                CreateRawItem(
                    "2021-03",
                    KeyValuePair.Create("01", new List<string> {"User2", "User4"}),
                    KeyValuePair.Create("02", new List<string> {"User1"}),
                    KeyValuePair.Create("03", new List<string> {"User1"})),
                CreateRawItem(
                    "2021-04",
                    KeyValuePair.Create("03", new List<string> {"User3"}))
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
                .Setup(r => r.GetReservations(yearMonth))
                .Returns(Task.FromResult((IReadOnlyCollection<RawItem>)mockResult));

        private static RawItem CreateRawItem(
            string monthKey,
            params KeyValuePair<string, List<string>>[] reservationData) =>
            new RawItem
            {
                PrimaryKey = "GLOBAL",
                SortKey = $"RESERVATIONS#{monthKey}",
                Reservations = new Dictionary<string, List<string>>(reservationData)
            };

        private static void CheckReservation(
            IEnumerable<Reservation> result,
            string expectedUserId,
            LocalDate expectedDate)
        {
            var actual = result.Where(r => r.UserId == expectedUserId && r.Date == expectedDate);

            Assert.Single(actual);
        }

        private static bool CheckRawItems(
            IReadOnlyCollection<RawItem> expected,
            IReadOnlyCollection<RawItem> actual) =>
            actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RawReservationsComparer()));

        private class RawReservationsComparer : IEqualityComparer<RawItem>
        {
            public bool Equals(RawItem first, RawItem second) =>
                first != null &&
                second != null &&
                first.PrimaryKey == second.PrimaryKey &&
                first.SortKey == second.SortKey &&
                CompareReservations(first.Reservations, second.Reservations);

            private static bool CompareReservations(
                IDictionary<string, List<string>> first,
                IDictionary<string, List<string>> second) =>
                first != null &&
                second != null &&
                first.Keys.Count == second.Keys.Count &&
                first.Keys.ToList().All(key => second.ContainsKey(key) && second[key].SequenceEqual(first[key]));

            public int GetHashCode(RawItem rawItem) =>
                HashCode.Combine(rawItem.PrimaryKey, rawItem.SortKey, rawItem.Reservations);
        }
    }
}
