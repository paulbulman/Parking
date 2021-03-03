namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Business;
    using Business.Data;
    using Model;
    using NodaTime;

    public class ReservationRepository : IReservationRepository
    {
        private readonly IDatabaseProvider databaseProvider;

        public ReservationRepository(IDatabaseProvider databaseProvider) => this.databaseProvider = databaseProvider;

        public async Task<IReadOnlyCollection<Reservation>> GetReservations(LocalDate firstDate, LocalDate lastDate)
        {
            var matchingReservations = new List<Reservation>();

            foreach (var yearMonth in new DateInterval(firstDate, lastDate).YearMonths())
            {
                var queryResult = await this.databaseProvider.GetReservations(yearMonth);

                var wholeMonthReservations =
                    queryResult.SelectMany(r => CreateWholeMonthReservations(yearMonth, r.Reservations));

                matchingReservations.AddRange(
                    wholeMonthReservations.Where(r => r.Date >= firstDate && r.Date <= lastDate));
            }

            return matchingReservations;
        }

        public async Task SaveReservations(IReadOnlyCollection<Reservation> reservations)
        {
            if (!reservations.Any())
            {
                return;
            }

            var orderedReservations = reservations.OrderBy(r => r.Date).ToList();

            var firstDate = orderedReservations.First().Date.With(DateAdjusters.StartOfMonth);
            var lastDate = orderedReservations.Last().Date.With(DateAdjusters.EndOfMonth);

            var existingReservations = await this.GetReservations(firstDate, lastDate);

            var combinedReservations = existingReservations
                .Where(existing => !IsOverwritten(existing, reservations))
                .Concat(reservations);

            var rawItems = combinedReservations
                .GroupBy(r => r.Date.ToYearMonth())
                .Select(CreateRawItem);

            await this.databaseProvider.SaveItems(rawItems);
        }

        private static IEnumerable<Reservation> CreateWholeMonthReservations(
            YearMonth yearMonth,
            IDictionary<string, List<string>> wholeMonthRawReservations) =>
            wholeMonthRawReservations.SelectMany(
                singleDayRawReservations => CreateSingleDayReservations(yearMonth, singleDayRawReservations));

        private static IEnumerable<Reservation> CreateSingleDayReservations(
            YearMonth yearMonth,
            KeyValuePair<string, List<string>> singleDayRawReservations) =>
            singleDayRawReservations.Value
                .Where(userId => userId != null)
                .Select(userId => new Reservation(userId, CreateLocalDate(yearMonth, singleDayRawReservations.Key)));

        private static LocalDate CreateLocalDate(YearMonth yearMonth, string dayKey) =>
            new LocalDate(yearMonth.Year, yearMonth.Month, int.Parse(dayKey));

        private static bool IsOverwritten(Reservation existingReservation, IEnumerable<Reservation> newReservations) =>
            newReservations.Any(updated => updated.Date == existingReservation.Date);

        private static RawItem CreateRawItem(IGrouping<YearMonth, Reservation> monthReservations) =>
            new RawItem
            {
                PrimaryKey = "GLOBAL",
                SortKey = $"RESERVATIONS#{monthReservations.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture)}",
                Reservations = CreateRawReservations(monthReservations)
            };

        private static Dictionary<string, List<string>> CreateRawReservations(IEnumerable<Reservation> monthReservations) =>
            monthReservations
                .GroupBy(r => r.Date)
                .ToDictionary(
                    g => g.Key.Day.ToString("D2", CultureInfo.InvariantCulture),
                    g => g.Select(r => r.UserId).ToList());
    }
}