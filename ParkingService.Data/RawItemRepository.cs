namespace ParkingService.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.CognitoIdentityProvider;
    using Amazon.CognitoIdentityProvider.Model;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.S3;
    using Amazon.S3.Model;
    using NodaTime;
    using NodaTime.Text;

    public interface IRawItemRepository
    {
        Task DeleteTriggerFiles(IEnumerable<string> keys);

        Task<string> GetConfiguration();

        Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth);

        Task<string> GetSchedules();

        Task<IReadOnlyCollection<string>> GetTriggerFileKeys();

        Task<IReadOnlyCollection<RawItem>> GetUsers();

        Task<IReadOnlyCollection<string>> GetUserIdsInGroup(string groupName);

        Task SaveItems(IEnumerable<RawItem> rawItems);

        Task SaveSchedules(string rawData);
        
        Task SendEmail(string rawData);
    }

    public class RawItemRepository : IRawItemRepository
    {
        private const string SchedulesObjectKey = "schedules.json";

        private readonly IAmazonCognitoIdentityProvider cognitoIdentityProvider;
        
        private readonly IAmazonDynamoDB dynamoDbClient;

        private readonly IAmazonS3 s3Client;

        public RawItemRepository(
            IAmazonCognitoIdentityProvider cognitoIdentityProvider,
            IAmazonDynamoDB dynamoDbClient,
            IAmazonS3 s3Client)
        {
            this.cognitoIdentityProvider = cognitoIdentityProvider;
            this.dynamoDbClient = dynamoDbClient;
            this.s3Client = s3Client;
        }

        private static string DataBucketName => Environment.GetEnvironmentVariable("DATA_BUCKET_NAME");

        private static string EmailBucketName => Environment.GetEnvironmentVariable("EMAIL_BUCKET_NAME");
        
        private static string TriggerBucketName => Environment.GetEnvironmentVariable("TRIGGER_BUCKET_NAME");

        private static string TableName => Environment.GetEnvironmentVariable("TABLE_NAME");
        
        private static string UserPoolId => Environment.GetEnvironmentVariable("USER_POOL_ID");

        public async Task DeleteTriggerFiles(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                await s3Client.DeleteObjectAsync(TriggerBucketName, key);
            }
        }

        public async Task<string> GetConfiguration() => await GetBucketData(DataBucketName, "configuration.json");

        public async Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth)
        {
            var hashKeyValue = $"REQUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await QuerySecondaryIndex(hashKeyValue);
        }

        public async Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth)
        {
            using var context = new DynamoDBContext(dynamoDbClient);

            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = TableName
            };

            const string HashKeyValue = "GLOBAL";
            var conditionValue = $"RESERVATIONS#{YearMonthPattern.Iso.Format(yearMonth)}";
            var query = context.QueryAsync<RawItem>(HashKeyValue, QueryOperator.Equal, new[] { conditionValue }, config);

            return await query.GetRemainingAsync();
        }

        public async Task<string> GetSchedules() => await GetBucketData(DataBucketName, SchedulesObjectKey);

        public async Task<IReadOnlyCollection<string>> GetTriggerFileKeys()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = TriggerBucketName
            };

            var objects = await s3Client.ListObjectsV2Async(request);

            return objects.S3Objects.Select(s => s.Key).ToArray();
        }

        public async Task<IReadOnlyCollection<RawItem>> GetUsers()
        {
            const string HashKeyValue = "PROFILE";

            return await QuerySecondaryIndex(HashKeyValue);
        }

        public async Task<IReadOnlyCollection<string>> GetUserIdsInGroup(string groupName)
        {
            var request = new ListUsersInGroupRequest
            {
                GroupName = groupName,
                UserPoolId = UserPoolId
            };
            
            var response = await this.cognitoIdentityProvider.ListUsersInGroupAsync(request);

            return response
                .Users
                .Select(u => u.Username)
                .ToArray();
        }

        public async Task SaveItems(IEnumerable<RawItem> rawItems)
        {
            using var context = new DynamoDBContext(dynamoDbClient);

            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = TableName
            };

            foreach (var rawItem in rawItems)
            {
                await context.SaveAsync(rawItem, config);
            }
        }

        public async Task SaveSchedules(string rawData) =>
            await SaveBucketData(DataBucketName, SchedulesObjectKey, rawData);

        public async Task SendEmail(string rawData) =>
            await SaveBucketData(EmailBucketName, Guid.NewGuid().ToString(), rawData);

        private async Task<string> GetBucketData(string bucketName, string objectKey)
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            using var response = await s3Client.GetObjectAsync(request);

            await using var responseStream = response.ResponseStream;

            using var reader = new StreamReader(responseStream);

            return await reader.ReadToEndAsync();
        }

        private async Task SaveBucketData(string bucketName, string objectKey, string rawData) =>
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                ContentBody = rawData
            });

        private async Task<IReadOnlyCollection<RawItem>> QuerySecondaryIndex(string hashKeyValue)
        {
            const string SecondaryIndexName = "SK-PK-index";

            using var context = new DynamoDBContext(dynamoDbClient);

            var config = new DynamoDBOperationConfig
            {
                IndexName = SecondaryIndexName,
                OverrideTableName = TableName
            };

            var query = context.QueryAsync<RawItem>(hashKeyValue, config);

            return await query.GetRemainingAsync();
        }
    }
}