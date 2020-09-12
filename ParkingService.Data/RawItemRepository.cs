namespace ParkingService.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using NodaTime;
    using NodaTime.Text;

    public interface IRawItemRepository
    {
        Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetUsers();
    }

    public class RawItemRepository : IRawItemRepository
    {
        private readonly IAmazonDynamoDB client;

        public RawItemRepository(IAmazonDynamoDB client) => this.client = client;

        private static string TableName => Environment.GetEnvironmentVariable("TABLE_NAME");

        public async Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth)
        {
            using var context = new DynamoDBContext(client);

            var config = new DynamoDBOperationConfig
            {
                IndexName = "SK-PK-index",
                OverrideTableName = TableName
            };

            var hashKeyValue = $"REQUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";
            var query = context.QueryAsync<RawItem>(hashKeyValue, config);

            return await query.GetRemainingAsync();
        }

        public async Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth)
        {
            using var context = new DynamoDBContext(client);

            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = TableName
            };

            const string HashKeyValue = "GLOBAL";
            var conditionValue = $"RESERVATIONS#{YearMonthPattern.Iso.Format(yearMonth)}";
            var query = context.QueryAsync<RawItem>(HashKeyValue, QueryOperator.Equal, new[] {conditionValue}, config);
            
            return await query.GetRemainingAsync();
        }

        public async Task<IReadOnlyCollection<RawItem>> GetUsers()
        {
            using var context = new DynamoDBContext(client);

            var config = new DynamoDBOperationConfig
            {
                IndexName = "SK-PK-index",
                OverrideTableName = TableName
            };

            const string HashKeyValue = "PROFILE";
            var query = context.QueryAsync<RawItem>(HashKeyValue, config);

            return await query.GetRemainingAsync();
        }
    }
}