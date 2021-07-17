namespace Parking.Data.Aws
{
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
        Task<RawItem> GetConfiguration();

        Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetRequests(string userId, YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth);

        Task<RawItem> GetSchedules();

        Task<IReadOnlyCollection<RawItem>> GetTriggers();

        Task<RawItem?> GetUser(string userId);

        Task<IReadOnlyCollection<RawItem>> GetUsers();

        Task SaveItem(RawItem rawItem);

        Task SaveItems(IEnumerable<RawItem> rawItems);

        Task DeleteItems(IEnumerable<RawItem> rawItems);
    }

    public class DatabaseProvider : IDatabaseProvider
    {
        private readonly IAmazonDynamoDB dynamoDbClient;

        public DatabaseProvider(IAmazonDynamoDB dynamoDbClient) => this.dynamoDbClient = dynamoDbClient;

        private static string TableName => Helpers.GetRequiredEnvironmentVariable("TABLE_NAME");

        public async Task<RawItem> GetConfiguration()
        {
            const string HashKeyValue = "GLOBAL";
            const string ConditionValue = "CONFIGURATION";

            var rawItems = await this.QueryPartitionKey(HashKeyValue, ConditionValue);

            return rawItems.Single();
        }

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

        public async Task<RawItem> GetSchedules()
        {
            const string HashKeyValue = "GLOBAL";
            const string ConditionValue = "SCHEDULES";

            var rawItems = await this.QueryPartitionKey(HashKeyValue, ConditionValue);

            return rawItems.Single();
        }

        public async Task<IReadOnlyCollection<RawItem>> GetTriggers()
        {
            const string HashKeyValue = "TRIGGER";

            return await this.QueryPartitionKey(HashKeyValue);
        }

        public async Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth)
        {
            const string HashKeyValue = "GLOBAL";
            var conditionValue = $"RESERVATIONS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await this.QueryPartitionKey(HashKeyValue, conditionValue);
        }
        public async Task<RawItem?> GetUser(string userId)
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

        public async Task DeleteItems(IEnumerable<RawItem> rawItems)
        {
            using var context = new DynamoDBContext(this.dynamoDbClient);

            var config = new DynamoDBOperationConfig { OverrideTableName = TableName };

            foreach (var rawItem in rawItems)
            {
                await context.DeleteAsync(rawItem, config);
            }
        }

        private async Task<IReadOnlyCollection<RawItem>> QueryPartitionKey(string hashKeyValue)
        {
            using var context = new DynamoDBContext(this.dynamoDbClient);

            var config = new DynamoDBOperationConfig { OverrideTableName = TableName };

            var query = context.QueryAsync<RawItem>(hashKeyValue, config);

            return await query.GetRemainingAsync();
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