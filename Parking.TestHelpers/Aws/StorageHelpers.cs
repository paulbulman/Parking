namespace Parking.TestHelpers.Aws
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Data;

    public static class StorageHelpers
    {
        private static string EmailBucketName => Helpers.GetRequiredEnvironmentVariable("EMAIL_BUCKET_NAME");

        public static IAmazonS3 CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonS3Config { ServiceURL = "http://localhost:4566", ForcePathStyle = true };

            return new AmazonS3Client(credentials, config);
        }

        public static async Task ResetStorage()
        {
            using var client = CreateClient();

            await DeleteEmailBucketIfExists(client);

            await client.PutBucketAsync(EmailBucketName);
        }

        public static async Task<IReadOnlyCollection<string>> GetSavedEmails()
        {
            using var client = CreateClient();

            var contents = await GetEmailBucketContents(client);

            var result = new List<string>();

            foreach (var s3Object in contents.S3Objects)
            {
                result.Add(await GetEmailFileContent(client, s3Object));
            }

            return result;
        }

        private static async Task DeleteEmailBucketIfExists(IAmazonS3 client)
        {
            var bucketsResponse = await client.ListBucketsAsync();

            if (bucketsResponse.Buckets.Exists(b => b.BucketName == EmailBucketName))
            {
                await DeleteEmailBucket(client);
            }
        }

        private static async Task DeleteEmailBucket(IAmazonS3 client)
        {
            var contents = await GetEmailBucketContents(client);

            foreach (var contentsS3Object in contents.S3Objects)
            {
                await client.DeleteObjectAsync(EmailBucketName, contentsS3Object.Key);
            }

            await client.DeleteBucketAsync(EmailBucketName);
        }

        private static async Task<ListObjectsV2Response> GetEmailBucketContents(IAmazonS3 client)
        {
            var listObjectsRequest = new ListObjectsV2Request { BucketName = EmailBucketName };

            return await client.ListObjectsV2Async(listObjectsRequest);
        }

        private static async Task<string> GetEmailFileContent(IAmazonS3 client, S3Object s3Object)
        {
            var response = await client.GetObjectAsync(EmailBucketName, s3Object.Key);

            await using var responseStream = response.ResponseStream;

            using var reader = new StreamReader(responseStream);

            return await reader.ReadToEndAsync();
        }
    }
}