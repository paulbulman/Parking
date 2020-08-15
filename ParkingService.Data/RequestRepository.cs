﻿namespace ParkingService.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public class RequestRepository
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
    }
}