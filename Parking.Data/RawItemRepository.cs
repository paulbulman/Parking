namespace Parking.Data
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
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;
    using Amazon.SimpleSystemsManagement;
    using Amazon.SimpleSystemsManagement.Model;
    using NodaTime;
    using NodaTime.Text;

    public interface IRawItemRepository
    {
        Task DeleteTriggerFiles(IEnumerable<string> keys);

        Task<string> GetConfiguration();

        Task<IReadOnlyCollection<RawItem>> GetRequests(YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetRequests(string userId, YearMonth yearMonth);

        Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth);

        Task<string> GetSchedules();

        Task<string> GetSmtpPassword();

        Task<IReadOnlyCollection<string>> GetTriggerFileKeys();

        Task<IReadOnlyCollection<RawItem>> GetUsers();

        Task<IReadOnlyCollection<string>> GetUserIdsInGroup(string groupName);

        Task SaveItems(IEnumerable<RawItem> rawItems);

        Task SaveSchedules(string rawData);

        Task SaveEmail(string rawData);

        Task SendNotification(string subject, string body);
    }

    public class RawItemRepository : IRawItemRepository
    {
        private const string SchedulesObjectKey = "schedules.json";

        private readonly IAmazonCognitoIdentityProvider cognitoIdentityProvider;

        private readonly IAmazonDynamoDB dynamoDbClient;

        private readonly IAmazonS3 s3Client;

        private readonly IAmazonSimpleNotificationService simpleNotificationService;

        private readonly IAmazonSimpleSystemsManagement simpleSystemsManagement;

        public RawItemRepository(
            IAmazonCognitoIdentityProvider cognitoIdentityProvider,
            IAmazonDynamoDB dynamoDbClient,
            IAmazonS3 s3Client,
            IAmazonSimpleNotificationService simpleNotificationService,
            IAmazonSimpleSystemsManagement simpleSystemsManagement)
        {
            this.cognitoIdentityProvider = cognitoIdentityProvider;
            this.dynamoDbClient = dynamoDbClient;
            this.s3Client = s3Client;
            this.simpleNotificationService = simpleNotificationService;
            this.simpleSystemsManagement = simpleSystemsManagement;
        }

        private static string DataBucketName => Environment.GetEnvironmentVariable("DATA_BUCKET_NAME");

        private static string EmailBucketName => Environment.GetEnvironmentVariable("EMAIL_BUCKET_NAME");

        private static string NotificationTopic => Environment.GetEnvironmentVariable("TOPIC_NAME");

        private static string SmtpPasswordKey => Environment.GetEnvironmentVariable("SMTP_PASSWORD_KEY");

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

        public async Task<IReadOnlyCollection<RawItem>> GetRequests(string userId, YearMonth yearMonth)
        {
            var hashKeyValue = $"USER#{userId}";
            var conditionValue = $"REQUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await QueryPartitionKey(hashKeyValue, conditionValue);
        }

        public async Task<IReadOnlyCollection<RawItem>> GetReservations(YearMonth yearMonth)
        {
            const string HashKeyValue = "GLOBAL";
            var conditionValue = $"RESERVATIONS#{YearMonthPattern.Iso.Format(yearMonth)}";

            return await QueryPartitionKey(HashKeyValue, conditionValue);
        }

        public async Task<string> GetSchedules() => await GetBucketData(DataBucketName, SchedulesObjectKey);

        public async Task<string> GetSmtpPassword()
        {
            var request = new GetParameterRequest { Name = SmtpPasswordKey, WithDecryption = true };

            var response = await this.simpleSystemsManagement.GetParameterAsync(request);

            return response.Parameter.Value;
        }

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

        public async Task SaveEmail(string rawData) =>
            await SaveBucketData(EmailBucketName, Guid.NewGuid().ToString(), rawData);

        public async Task SendNotification(string subject, string body) =>
            await this.simpleNotificationService.PublishAsync(new PublishRequest(NotificationTopic, body, subject));

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

        private async Task<IReadOnlyCollection<RawItem>> QueryPartitionKey(string hashKeyValue, string conditionValue)
        {
            using var context = new DynamoDBContext(dynamoDbClient);

            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = TableName
            };

            var query = context.QueryAsync<RawItem>(hashKeyValue, QueryOperator.Equal, new[] { conditionValue }, config);

            return await query.GetRemainingAsync();
        }

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