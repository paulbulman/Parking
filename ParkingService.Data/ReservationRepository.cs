namespace ParkingService.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public class ReservationRepository
    {
        private readonly IAmazonDynamoDB client;

        public ReservationRepository(IAmazonDynamoDB client) => this.client = client;

        private static string TableName => Environment.GetEnvironmentVariable("TABLE_NAME");

        public async Task<IReadOnlyCollection<Reservation>> GetReservations(LocalDate firstDate, LocalDate lastDate)
        {
            var yearMonths = Enumerable.Range(0, Period.Between(firstDate, lastDate, PeriodUnits.Days).Days + 1)
                .Select(offset => firstDate.PlusDays(offset).ToYearMonth())
                .Distinct();

            var context = new DynamoDBContext(client);

            var matchingReservations = new List<Reservation>();

            foreach (var yearMonth in yearMonths)
            {
                var config = new DynamoDBOperationConfig
                {
                    OverrideTableName = TableName
                };

                var query = CreateQuery(context, yearMonth, config);
                var queryResult = await query.GetRemainingAsync();

                var wholeMonthReservations = queryResult.SelectMany(r => CreateWholeMonthReservations(yearMonth, r.Reservations));

                matchingReservations.AddRange(wholeMonthReservations.Where(r => r.Date >= firstDate && r.Date <= lastDate));
            }

            return matchingReservations;
        }

        private static AsyncSearch<Item> CreateQuery(IDynamoDBContext context, YearMonth yearMonth, DynamoDBOperationConfig config)
        {
            const string HashKeyValue = "GLOBAL";
            var conditionValue = $"RESERVATIONS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return context.QueryAsync<Item>(HashKeyValue, QueryOperator.Equal, new[] { conditionValue }, config);
        }

        private static IEnumerable<Reservation> CreateWholeMonthReservations(YearMonth yearMonth, IDictionary<string, List<string>> wholeMonthRawReservations) =>
            wholeMonthRawReservations
                .SelectMany(singleDayRawReservations => CreateSingleDayReservations(yearMonth, singleDayRawReservations));

        private static IEnumerable<Reservation> CreateSingleDayReservations(YearMonth yearMonth, KeyValuePair<string, List<string>> singleDayRawReservations)
        {
            return singleDayRawReservations.Value
                .Where(userId => userId != null)
                .Select(userId => new Reservation(userId, CreateLocalDate(yearMonth, singleDayRawReservations.Key)));
        }

        private static LocalDate CreateLocalDate(YearMonth yearMonth, string dayKey) => 
            new LocalDate(yearMonth.Year, yearMonth.Month, int.Parse(dayKey));
    }
}