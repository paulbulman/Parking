namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Model;
    using NodaTime;

    public class ReservationRepository : IReservationRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public ReservationRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task<IReadOnlyCollection<Reservation>> GetReservations(LocalDate firstDate, LocalDate lastDate)
        {
            var matchingReservations = new List<Reservation>();

            foreach (var yearMonth in new DateInterval(firstDate, lastDate).YearMonths())
            {
                var queryResult = await rawItemRepository.GetReservations(yearMonth);

                var wholeMonthReservations =
                    queryResult.SelectMany(r => CreateWholeMonthReservations(yearMonth, r.Reservations));

                matchingReservations.AddRange(
                    wholeMonthReservations.Where(r => r.Date >= firstDate && r.Date <= lastDate));
            }

            return matchingReservations;
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
    }
}