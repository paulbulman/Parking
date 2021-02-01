namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Model;
    using NodaTime;

    public class RequestRepository : IRequestRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public RequestRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task<IReadOnlyCollection<Request>> GetRequests(LocalDate firstDate, LocalDate lastDate)
        {
            var yearMonths = Enumerable.Range(0, Period.Between(firstDate, lastDate, PeriodUnits.Days).Days + 1)
                .Select(offset => firstDate.PlusDays(offset).ToYearMonth())
                .Distinct();

            var matchingRequests = new List<Request>();

            foreach (var yearMonth in yearMonths)
            {
                var queryResult = await rawItemRepository.GetRequests(yearMonth);

                var wholeMonthRequests =
                    queryResult.SelectMany(r => CreateWholeMonthRequests(r.PrimaryKey, yearMonth, r.Requests));

                matchingRequests.AddRange(wholeMonthRequests.Where(r => r.Date >= firstDate && r.Date <= lastDate));
            }

            return matchingRequests;
        }

        public async Task SaveRequests(IReadOnlyCollection<Request> requests)
        {
            if (!requests.Any())
            {
                return;
            }

            var orderedRequests = requests.OrderBy(r => r.Date).ToList();

            var firstDate = orderedRequests.First().Date.With(DateAdjusters.StartOfMonth);
            var lastDate = orderedRequests.Last().Date.With(DateAdjusters.EndOfMonth);

            var existingRequests = await GetRequests(firstDate, lastDate);

            var combinedRequests = existingRequests
                .Where(existingRequest => !IsOverwritten(existingRequest, requests))
                .Concat(requests)
                .ToList();

            await SaveCombinedRequests(combinedRequests);
        }

        private static bool IsOverwritten(Request existingRequest, IEnumerable<Request> newRequests) =>
            newRequests.Any(r => r.UserId == existingRequest.UserId && r.Date == existingRequest.Date);

        private async Task SaveCombinedRequests(IReadOnlyCollection<Request> requests)
        {
            var rawItems = new List<RawItem>();

            foreach (var userRequests in requests.GroupBy(r => r.UserId))
            {
                var userId = userRequests.Key;

                var userMonthRequests = userRequests
                    .GroupBy(request => request.Date.ToYearMonth())
                    .Select(yearMonthRequests => CreateRawItem(userId, yearMonthRequests));

                rawItems.AddRange(userMonthRequests);
            }

            await rawItemRepository.SaveItems(rawItems);
        }

        private static IEnumerable<Request> CreateWholeMonthRequests(
            string primaryKey,
            YearMonth yearMonth,
            IDictionary<string, string> wholeMonthRawRequests)
        {
            var userId = primaryKey.Split('#')[1];

            return wholeMonthRawRequests.Select(singleDayRawRequest =>
                CreateRequest(yearMonth, userId, singleDayRawRequest));
        }

        private static Request CreateRequest(
            YearMonth yearMonth,
            string userId,
            KeyValuePair<string, string> rawRequest)
        {
            var (dayKey, rawRequestStatus) = rawRequest;
            var requestStatus = CreateRequestStatus(rawRequestStatus);

            return new Request(userId, CreateLocalDate(yearMonth, dayKey), requestStatus);
        }

        private static RequestStatus CreateRequestStatus(string rawRequestStatus) =>
            rawRequestStatus switch
            {
                "REQUESTED" => RequestStatus.Requested,
                "ALLOCATED" => RequestStatus.Allocated,
                "CANCELLED" => RequestStatus.Cancelled,
                _ => throw new ArgumentOutOfRangeException(nameof(rawRequestStatus))
            };

        private static LocalDate CreateLocalDate(YearMonth yearMonth, string dayKey) =>
            new LocalDate(yearMonth.Year, yearMonth.Month, int.Parse(dayKey));

        private static RawItem CreateRawItem(string userId, IGrouping<YearMonth, Request> yearMonthRequests)
        {
            return new RawItem
            {
                PrimaryKey = $"USER#{userId}",
                SortKey = $"REQUESTS#{yearMonthRequests.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture)}",
                Requests = yearMonthRequests.ToDictionary(CreateRawRequestDayKey, CreateRawRequestStatus)
            };
        }

        private static string CreateRawRequestDayKey(Request request) =>
            request.Date.Day.ToString("D2", CultureInfo.InvariantCulture);

        private static string CreateRawRequestStatus(Request request) =>
            request.Status switch
            {
                RequestStatus.Requested => "REQUESTED",
                RequestStatus.Allocated => "ALLOCATED",
                RequestStatus.Cancelled => "CANCELLED",
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };
    }
}
