namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Business;
    using Business.Data;
    using Microsoft.Extensions.Logging;
    using Model;
    using NodaTime;

    public class ReservationRepository : IReservationRepository
    {
        private readonly ILogger<ReservationRepository> logger;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IUserRepository userRepository;

        public ReservationRepository(
            ILogger<ReservationRepository> logger,
            IDatabaseProvider databaseProvider,
            IUserRepository userRepository)
        {
            this.logger = logger;
            this.databaseProvider = databaseProvider;
            this.userRepository = userRepository;
        }

        public async Task<IReadOnlyCollection<Reservation>> GetReservations(LocalDate firstDate, LocalDate lastDate)
        {
            var users = await this.userRepository.GetUsers();

            var matchingReservations = new List<Reservation>();

            foreach (var yearMonth in new DateInterval(firstDate, lastDate).YearMonths())
            {
                var queryResult = await this.databaseProvider.GetReservations(yearMonth);

                var wholeMonthReservations =
                    queryResult.SelectMany(r => CreateWholeMonthReservations(r, yearMonth));

                matchingReservations.AddRange(
                    wholeMonthReservations.Where(r =>
                        r.Date >= firstDate && r.Date <= lastDate && users.Select(u => u.UserId).Contains(r.UserId)));
            }

            return matchingReservations;
        }

        public async Task SaveReservations(IReadOnlyCollection<Reservation> reservations, IReadOnlyCollection<User> users)
        {
            if (!reservations.Any())
            {
                return;
            }

            var fullNames = users.ToDictionary(u => u.UserId, u => $"{u.FirstName} {u.LastName}");

            this.logger.LogDebug(
                "Saving reservations: {@reservations}",
                reservations.Select(r => new { r.UserId, FullName = GetFullName(fullNames, r.UserId), r.Date }));

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

        private static string GetFullName(IDictionary<string, string> fullNames, string userId) =>
            string.IsNullOrEmpty(userId) ? "[No user]" :
            !fullNames.ContainsKey(userId) ? $"[Unknown user ID '{userId}']" :
            fullNames[userId];

        private static IEnumerable<Reservation> CreateWholeMonthReservations(RawItem rawItem, YearMonth yearMonth)
        {
            var wholeMonthRawReservations = rawItem.Reservations;

            if (wholeMonthRawReservations == null)
            {
                throw new InvalidOperationException("Raw reservations cannot be null.");
            }

            return wholeMonthRawReservations.SelectMany(
                singleDayRawReservations => CreateSingleDayReservations(yearMonth, singleDayRawReservations));
        }

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
            RawItem.CreateReservations(
                primaryKey: "GLOBAL",
                sortKey: $"RESERVATIONS#{monthReservations.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture)}",
                reservations: CreateRawReservations(monthReservations));

        private static Dictionary<string, List<string>> CreateRawReservations(IEnumerable<Reservation> monthReservations) =>
            monthReservations
                .GroupBy(r => r.Date)
                .ToDictionary(
                    g => g.Key.Day.ToString("D2", CultureInfo.InvariantCulture),
                    g => g.Select(r => r.UserId).Where(u => !string.IsNullOrEmpty(u)).ToList());
    }
}