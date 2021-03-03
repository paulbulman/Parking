namespace Parking.Data.Aws
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using NodaTime;
    using NodaTime.Text;

    public interface IDatabaseProvider
    {
        Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth);
        
        Task<IReadOnlyCollection<RawItem>> GetRequests(string userId, YearMonth yearMonth);
        
        Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth);
        
        Task<RawItem> GetUser(string userId);
        
        Task<IReadOnlyCollection<RawItem>> GetUsers();
        
        Task SaveItem(RawItem rawItem);
        
        Task SaveItems(IEnumerable<RawItem> rawItems);
    }

    public class DatabaseProvider : IDatabaseProvider
    {
        private readonly IAmazonDynamoDB dynamoDbClient;

        public DatabaseProvider(IAmazonDynamoDB dynamoDbClient) => this.dynamoDbClient = dynamoDbClient;
        
        private static string TableName => Environment.GetEnvironmentVariable("TABLE_NAME");

        public async Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth)
        {
            var hashKeyValue = $"REQUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await this.QuerySecondaryIndex(hashKeyValue);
        }

        public async Task<IReadOnlyCollection<RawItem>> GetRequests(string userId, YearMonth yearMonth)
        {
            var hashKeyValue = $"USER#{userId}";
            var conditionValue = $"REQUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await this.QueryPartitionKey(hashKeyValue, conditionValue);
        }

        public async Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth)
        {
            const string HashKeyValue = "GLOBAL";
            var conditionValue = $"RESERVATIONS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await this.QueryPartitionKey(HashKeyValue, conditionValue);
        }
        public async Task<RawItem> GetUser(string userId)
        {
            var hashKeyValue = $"USER#{userId}";
            const string ConditionValue = "PROFILE";

            var result = await this.QueryPartitionKey(hashKeyValue, ConditionValue);

            return result.FirstOrDefault();
        }

        public async Task<IReadOnlyCollection<RawItem>> GetUsers()
        {
            const string HashKeyValue = "PROFILE";

            return await this.QuerySecondaryIndex(HashKeyValue);
        }

        public async Task SaveItem(RawItem rawItem)
        {
            using var context = new DynamoDBContext(this.dynamoDbClient);

            var config = new DynamoDBOperationConfig { OverrideTableName = TableName };

            await context.SaveAsync(rawItem, config);
        }

        public async Task SaveItems(IEnumerable<RawItem> rawItems)
        {
            using var context = new DynamoDBContext(this.dynamoDbClient);

            var config = new DynamoDBOperationConfig { OverrideTableName = TableName };

            foreach (var rawItem in rawItems)
            {
                await context.SaveAsync(rawItem, config);
            }
        }

        private async Task<IReadOnlyCollection<RawItem>> QueryPartitionKey(string hashKeyValue, string conditionValue)
        {
            using var context = new DynamoDBContext(this.dynamoDbClient);

            var config = new DynamoDBOperationConfig { OverrideTableName = TableName };

            var query = context.QueryAsync<RawItem>(hashKeyValue, QueryOperator.Equal, new[] { conditionValue }, config);

            return await query.GetRemainingAsync();
        }

        private async Task<IReadOnlyCollection<RawItem>> QuerySecondaryIndex(string hashKeyValue)
        {
            const string SecondaryIndexName = "SK-PK-index";

            using var context = new DynamoDBContext(this.dynamoDbClient);

            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = TableName,
                IndexName = SecondaryIndexName
            };

            var query = context.QueryAsync<RawItem>(hashKeyValue, config);

            return await query.GetRemainingAsync();
        }
    }
}