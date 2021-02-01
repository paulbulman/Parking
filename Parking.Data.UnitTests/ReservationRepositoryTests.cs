namespace Parking.Data.UnitTests
{
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
        public static async void Converts_raw_items_to_reservations()
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
        public static async void Filters_reservations_outside_specified_date_range()
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
    }
}
