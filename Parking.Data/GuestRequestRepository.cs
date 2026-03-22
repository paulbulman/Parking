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

    public class GuestRequestRepository(
        ILogger<GuestRequestRepository> logger,
        IDatabaseProvider databaseProvider) : IGuestRequestRepository
    {
        public async Task<IReadOnlyCollection<GuestRequest>> GetGuestRequests(DateInterval dateInterval)
        {
            var matchingGuestRequests = new List<GuestRequest>();

            foreach (var yearMonth in dateInterval.YearMonths())
            {
                var queryResult = await databaseProvider.GetGuests(yearMonth);

                var wholeMonthGuests = queryResult.SelectMany(r => CreateWholeMonthGuestRequests(r, yearMonth));

                matchingGuestRequests.AddRange(wholeMonthGuests.Where(g => dateInterval.Contains(g.Date)));
            }

            return matchingGuestRequests;
        }

        public async Task SaveGuestRequest(GuestRequest guestRequest)
        {
            await SaveGuestRequests([guestRequest]);
        }

        public async Task SaveGuestRequests(IReadOnlyCollection<GuestRequest> guestRequests)
        {
            if (!guestRequests.Any())
            {
                return;
            }

            logger.LogDebug(
                "Saving guest requests: {@guestRequests}",
                guestRequests.Select(g => new { g.Id, g.Name, g.Date, g.Status }));

            var yearMonths = guestRequests.Select(g => g.Date.ToYearMonth()).Distinct();

            foreach (var yearMonth in yearMonths)
            {
                var existingGuests = await GetGuestRequestsForMonth(yearMonth);

                var monthGuests = guestRequests.Where(g => g.Date.ToYearMonth() == yearMonth).ToList();

                var combinedGuests = existingGuests
                    .Where(existing => !monthGuests.Any(g => g.Id == existing.Id))
                    .Concat(monthGuests)
                    .ToList();

                var rawItem = CreateRawItem(yearMonth, combinedGuests);
                await databaseProvider.SaveItem(rawItem);
            }
        }

        public async Task UpdateGuestRequest(GuestRequest guestRequest)
        {
            await SaveGuestRequests([guestRequest]);
        }

        public async Task DeleteGuestRequest(LocalDate date, string id)
        {
            var yearMonth = date.ToYearMonth();
            var existingGuests = await GetGuestRequestsForMonth(yearMonth);

            var updatedGuests = existingGuests.Where(g => g.Id != id).ToList();

            var rawItem = CreateRawItem(yearMonth, updatedGuests);
            await databaseProvider.SaveItem(rawItem);
        }

        private async Task<IReadOnlyCollection<GuestRequest>> GetGuestRequestsForMonth(YearMonth yearMonth)
        {
            var firstDate = yearMonth.OnDayOfMonth(1);
            var lastDate = yearMonth.OnDayOfMonth(CalendarSystem.Iso.GetDaysInMonth(yearMonth.Year, yearMonth.Month));
            var dateInterval = new DateInterval(firstDate, lastDate);

            return await GetGuestRequests(dateInterval);
        }

        private static IEnumerable<GuestRequest> CreateWholeMonthGuestRequests(RawItem rawItem, YearMonth yearMonth)
        {
            var wholeMonthRawGuests = rawItem.Guests;

            if (wholeMonthRawGuests == null)
            {
                throw new InvalidOperationException("Raw guests cannot be null.");
            }

            return wholeMonthRawGuests.SelectMany(
                singleDayRawGuests => CreateSingleDayGuestRequests(yearMonth, singleDayRawGuests));
        }

        private static IEnumerable<GuestRequest> CreateSingleDayGuestRequests(
            YearMonth yearMonth,
            KeyValuePair<string, List<GuestData>> singleDayRawGuests)
        {
            var date = new LocalDate(yearMonth.Year, yearMonth.Month, int.Parse(singleDayRawGuests.Key));

            return singleDayRawGuests.Value.Select(guest => new GuestRequest(
                id: guest.Id,
                date: date,
                name: guest.Name,
                visitingUserId: guest.VisitingUserId,
                registrationNumber: guest.RegistrationNumber,
                status: CreateGuestRequestStatus(guest.Status)));
        }

        private static GuestRequestStatus CreateGuestRequestStatus(string rawStatus) =>
            rawStatus switch
            {
                "A" => GuestRequestStatus.Allocated,
                "I" => GuestRequestStatus.Interrupted,
                "P" => GuestRequestStatus.Pending,
                _ => throw new ArgumentException($"Unrecognised guest request status: {rawStatus}")
            };

        private static string CreateRawGuestRequestStatus(GuestRequestStatus status) =>
            status switch
            {
                GuestRequestStatus.Allocated => "A",
                GuestRequestStatus.Interrupted => "I",
                GuestRequestStatus.Pending => "P",
                _ => throw new ArgumentException($"Unrecognised guest request status: {status}")
            };

        private static RawItem CreateRawItem(YearMonth yearMonth, IReadOnlyCollection<GuestRequest> guestRequests)
        {
            var rawGuests = guestRequests
                .GroupBy(g => g.Date)
                .ToDictionary(
                    g => g.Key.Day.ToString("D2", CultureInfo.InvariantCulture),
                    g => g.Select(guest => new GuestData
                    {
                        Id = guest.Id,
                        Name = guest.Name,
                        VisitingUserId = guest.VisitingUserId,
                        RegistrationNumber = guest.RegistrationNumber,
                        Status = CreateRawGuestRequestStatus(guest.Status),
                    }).ToList());

            return RawItem.CreateGuests(
                primaryKey: "GLOBAL",
                sortKey: $"GUESTS#{yearMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture)}",
                guests: rawGuests);
        }
    }
}
