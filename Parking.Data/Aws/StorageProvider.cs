namespace Parking.Data.Aws
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    public interface IStorageProvider
    {
        Task<string> GetConfiguration();
        
        Task DeleteTriggerFiles(IEnumerable<string> keys);
        
        Task<string> GetSchedules();
        
        Task<IReadOnlyCollection<string>> GetTriggerFileKeys();
        
        Task SaveSchedules(string rawData);
        
        Task SaveEmail(string rawData);
        
        Task SaveTrigger();
    }

    public class StorageProvider : IStorageProvider
    {
        private const string SchedulesObjectKey = "schedules.json";
        
        private readonly IAmazonS3 s3Client;

        public StorageProvider(IAmazonS3 s3Client) => this.s3Client = s3Client;

        private static string DataBucketName => Helpers.GetRequiredEnvironmentVariable("DATA_BUCKET_NAME");

        private static string EmailBucketName => Helpers.GetRequiredEnvironmentVariable("EMAIL_BUCKET_NAME");

        private static string TriggerBucketName => Helpers.GetRequiredEnvironmentVariable("TRIGGER_BUCKET_NAME");

        public async Task<string> GetConfiguration() => await this.GetBucketData(DataBucketName, "configuration.json");

        public async Task DeleteTriggerFiles(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                await this.s3Client.DeleteObjectAsync(TriggerBucketName, key);
            }
        }

        public async Task<string> GetSchedules() => await this.GetBucketData(DataBucketName, SchedulesObjectKey);

        public async Task<IReadOnlyCollection<string>> GetTriggerFileKeys()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = TriggerBucketName
            };

            var objects = await this.s3Client.ListObjectsV2Async(request);

            return objects.S3Objects.Select(s => s.Key).ToArray();
        }

        public async Task SaveSchedules(string rawData) =>
            await this.SaveBucketData(DataBucketName, SchedulesObjectKey, rawData);

        public async Task SaveEmail(string rawData) =>
            await this.SaveBucketData(EmailBucketName, Guid.NewGuid().ToString(), rawData);

        public async Task SaveTrigger() =>
            await this.SaveBucketData(TriggerBucketName, Guid.NewGuid().ToString(), string.Empty);

        private async Task<string> GetBucketData(string bucketName, string objectKey)
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            using var response = await this.s3Client.GetObjectAsync(request);

            await using var responseStream = response.ResponseStream;

            using var reader = new StreamReader(responseStream);

            return await reader.ReadToEndAsync();
        }

        private async Task SaveBucketData(string bucketName, string objectKey, string rawData) =>
            await this.s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                ContentBody = rawData
            });
    }
}