namespace Parking.Api.IntegrationTests.Helpers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;

    public static class StorageHelpers
    {
        private static string DataBucketName => Environment.GetEnvironmentVariable("DATA_BUCKET_NAME");

        private static string TriggerBucketName => Environment.GetEnvironmentVariable("TRIGGER_BUCKET_NAME");

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
            await client.PutBucketAsync(TriggerBucketName);
        }

        public static async Task<int> GetTriggerFileCount()
        {
            using var client = CreateClient();

            var listObjectsRequest = new ListObjectsV2Request { BucketName = TriggerBucketName };

            var contents = await client.ListObjectsV2Async(listObjectsRequest);

            return contents.S3Objects.Count;
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
            var listObjectsRequest = new ListObjectsV2Request {BucketName = bucketName};

            var contents = await client.ListObjectsV2Async(listObjectsRequest);

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
    }
}