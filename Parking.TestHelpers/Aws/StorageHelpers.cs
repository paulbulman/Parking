namespace Parking.TestHelpers.Aws
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Data;

    public static class StorageHelpers
    {
        private static string DataBucketName => Helpers.GetRequiredEnvironmentVariable("DATA_BUCKET_NAME");

        private static string EmailBucketName => Helpers.GetRequiredEnvironmentVariable("EMAIL_BUCKET_NAME");

        private static string TriggerBucketName => Helpers.GetRequiredEnvironmentVariable("TRIGGER_BUCKET_NAME");

        public static IAmazonS3 CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonS3Config { ServiceURL = "http://localhost:4566", ForcePathStyle = true };

            return new AmazonS3Client(credentials, config);
        }

        public static async Task ResetStorage()
        {
            using var client = CreateClient();

            await DeleteBuckets(client);

            await client.PutBucketAsync(DataBucketName);
            await client.PutBucketAsync(EmailBucketName);
            await client.PutBucketAsync(TriggerBucketName);
        }

        public static async Task CreateTrigger()
        {
            using var client = CreateClient();

            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = TriggerBucketName,
                Key = "__TRIGGER__"
            });
        }

        public static async Task<IReadOnlyCollection<string>> GetSavedEmails()
        {
            using var client = CreateClient();

            var contents = await GetBucketContents(client, EmailBucketName);

            var result = new List<string>();

            foreach (var s3Object in contents.S3Objects)
            {
                result.Add(await GetFileContent(client, s3Object));
            }

            return result;
        }

        public static async Task<int> GetTriggerFileCount()
        {
            using var client = CreateClient();

            var contents = await GetBucketContents(client, TriggerBucketName);

            return contents.S3Objects.Count;
        }

        public static async Task SaveSchedules(string rawData)
        {
            using var client = CreateClient();

            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = DataBucketName,
                Key = "schedules.json",
                ContentBody = rawData
            });
        }

        public static async Task SaveConfiguration(string rawData)
        {
            using var client = CreateClient();

            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = DataBucketName,
                Key = "configuration.json",
                ContentBody = rawData
            });
        }

        private static async Task DeleteBuckets(IAmazonS3 client)
        {
            var bucketsResponse = await client.ListBucketsAsync();

            foreach (var bucketName in bucketsResponse.Buckets.Select(b => b.BucketName))
            {
                await DeleteBucket(client, bucketName);
            }
        }

        private static async Task DeleteBucket(IAmazonS3 client, string bucketName)
        {
            var contents = await GetBucketContents(client, bucketName);

            foreach (var contentsS3Object in contents.S3Objects)
            {
                await client.DeleteObjectAsync(contentsS3Object.BucketName, contentsS3Object.Key);
            }

            await client.DeleteBucketAsync(bucketName);
        }

        public static async Task CreateConfiguration(string configuration)
        {
            using var client = CreateClient();

            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = DataBucketName,
                ContentBody = configuration,
                Key = "configuration.json"
            });
        }

        private static async Task<ListObjectsV2Response> GetBucketContents(IAmazonS3 client, string bucketName)
        {
            var listObjectsRequest = new ListObjectsV2Request { BucketName = bucketName };

            return await client.ListObjectsV2Async(listObjectsRequest);
        }

        private static async Task<string> GetFileContent(IAmazonS3 client, S3Object s3Object)
        {
            var response = await client.GetObjectAsync(s3Object.BucketName, s3Object.Key);

            await using var responseStream = response.ResponseStream;

            using var reader = new StreamReader(responseStream);

            return await reader.ReadToEndAsync();
        }
    }
}