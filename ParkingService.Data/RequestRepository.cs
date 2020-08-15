namespace ParkingService.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public class RequestRepository
    {
        private readonly IAmazonDynamoDB client;

        public RequestRepository(IAmazonDynamoDB client) => this.client = client;

        private static string TableName => Environment.GetEnvironmentVariable("TABLE_NAME");

        public async Task<IReadOnlyCollection<Request>> GetRequests(LocalDate firstDate, LocalDate lastDate)
        {
            var yearMonths = Enumerable.Range(0, Period.Between(firstDate, lastDate, PeriodUnits.Days).Days + 1)
                .Select(offset => firstDate.PlusDays(offset).ToYearMonth())
                .Distinct();

            var context = new DynamoDBContext(client);

            var matchingRequests = new List<Request>();

            foreach (var yearMonth in yearMonths)
            {
                var config = new DynamoDBOperationConfig
                {
                    IndexName = "SK-PK-index",
                    OverrideTableName = TableName
                };

                var query = CreateQuery(context, yearMonth, config);
                var queryResult = await query.GetRemainingAsync();

                var wholeMonthRequests = queryResult.SelectMany(r => CreateWholeMonthRequests(r.PrimaryKey, yearMonth, r.Requests));

                matchingRequests.AddRange(wholeMonthRequests.Where(r => r.Date >= firstDate && r.Date <= lastDate));
            }

            return matchingRequests;
        }

        private static AsyncSearch<Item> CreateQuery(DynamoDBContext context, YearMonth yearMonth, DynamoDBOperationConfig config)
        {
            var hashKeyValue = $"REQUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";
            
            return context.QueryAsync<Item>(hashKeyValue, config);
        }

        private static IEnumerable<Request> CreateWholeMonthRequests(string primaryKey, YearMonth yearMonth, IDictionary<string, string> wholeMonthRawRequests)
        {
            var userId = primaryKey.Split('#')[1];

            return wholeMonthRawRequests.Select(singleDayRawRequest => CreateRequest(yearMonth, userId, singleDayRawRequest));
        }

        private static Request CreateRequest(YearMonth yearMonth, string userId, KeyValuePair<string, string> rawRequest)
        {
            var requestStatus = CreateRequestStatus(rawRequest.Value);

            return new Request(userId, CreateLocalDate(yearMonth, rawRequest.Key), requestStatus);
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
