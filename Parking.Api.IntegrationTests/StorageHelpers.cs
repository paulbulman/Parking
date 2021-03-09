namespace Parking.Api.IntegrationTests
{
    using System;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;

    public static class StorageHelpers
    {
        private static string DataBucketName => Environment.GetEnvironmentVariable("DATA_BUCKET_NAME");

        public static IAmazonS3 CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonS3Config {ServiceURL = "http://localhost:4566", ForcePathStyle = true};

            return new AmazonS3Client(credentials, config);
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