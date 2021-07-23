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
    using Model;
    using NodaTime;

    public class RequestRepository : IRequestRepository
    {
        private readonly IDatabaseProvider databaseProvider;

        public RequestRepository(IDatabaseProvider databaseProvider) => this.databaseProvider = databaseProvider;

        public async Task<IReadOnlyCollection<Request>> GetRequests(LocalDate firstDate, LocalDate lastDate)
        {
            var requests = new List<Request>();

            foreach (var yearMonth in new DateInterval(firstDate, lastDate).YearMonths())
            {
                var queryResult = await this.databaseProvider.GetRequests(yearMonth);

                requests.AddRange(CreateFilteredRequests(queryResult, yearMonth, firstDate, lastDate));
            }

            return requests;
        }

        public async Task<IReadOnlyCollection<Request>> GetRequests(string userId, LocalDate firstDate, LocalDate lastDate)
        {
            var requests = new List<Request>();

            foreach (var yearMonth in new DateInterval(firstDate, lastDate).YearMonths())
            {
                var queryResult = await this.databaseProvider.GetRequests(userId, yearMonth);

                requests.AddRange(CreateFilteredRequests(queryResult, yearMonth, firstDate, lastDate));
            }

            return requests;
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

            var isSingleUser = orderedRequests.Select(r => r.UserId).Distinct().Count() == 1;

            var existingRequests = isSingleUser
                ? await this.GetRequests(orderedRequests.First().UserId, firstDate, lastDate)
                : await this.GetRequests(firstDate, lastDate);

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

            await this.databaseProvider.SaveItems(rawItems);
        }

        private static IReadOnlyCollection<Request> CreateFilteredRequests(
            IReadOnlyCollection<RawItem> rawItems,
            YearMonth yearMonth,
            LocalDate firstDate,
            LocalDate lastDate) =>
            rawItems.SelectMany(r => CreateWholeMonthRequests(r, yearMonth))
                .Where(r => r.Date >= firstDate && r.Date <= lastDate)
                .ToArray();

        private static IEnumerable<Request> CreateWholeMonthRequests(RawItem rawItem, YearMonth yearMonth)
        {
            var userId = rawItem.PrimaryKey.Split('#')[1];

            var wholeMonthRawRequests = rawItem.Requests;

            if (wholeMonthRawRequests == null)
            {
                throw new InvalidOperationException("Raw requests cannot be null.");
            }

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
                "R" => RequestStatus.Requested,
                "A" => RequestStatus.Allocated,
                "C" => RequestStatus.Cancelled,
                "S" => RequestStatus.SoftInterrupted,
                "H" => RequestStatus.HardInterrupted,
                _ => throw new ArgumentOutOfRangeException(nameof(rawRequestStatus))
            };

        private static LocalDate CreateLocalDate(YearMonth yearMonth, string dayKey) =>
            new LocalDate(yearMonth.Year, yearMonth.Month, int.Parse(dayKey));

        private static RawItem CreateRawItem(string userId, IGrouping<YearMonth, Request> yearMonthRequests) =>
            RawItem.CreateRequests(
                primaryKey: $"USER#{userId}",
                sortKey: $"REQUESTS#{yearMonthRequests.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture)}",
                requests: yearMonthRequests.ToDictionary(CreateRawRequestDayKey, CreateRawRequestStatus));

        private static string CreateRawRequestDayKey(Request request) =>
            request.Date.Day.ToString("D2", CultureInfo.InvariantCulture);

        private static string CreateRawRequestStatus(Request request) =>
            request.Status switch
            {
                RequestStatus.Requested => "R",
                RequestStatus.Allocated => "A",
                RequestStatus.Cancelled => "C",
                RequestStatus.SoftInterrupted => "S",
                RequestStatus.HardInterrupted => "H",
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };
    }
}
