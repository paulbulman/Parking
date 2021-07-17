namespace Parking.Data.Aws
{
    using System;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    public interface IStorageProvider
    {
        Task SaveEmail(string rawData);
    }

    public class StorageProvider : IStorageProvider
    {
        private readonly IAmazonS3 s3Client;

        public StorageProvider(IAmazonS3 s3Client) => this.s3Client = s3Client;

        private static string EmailBucketName => Helpers.GetRequiredEnvironmentVariable("EMAIL_BUCKET_NAME");

        public async Task SaveEmail(string rawData) =>
            await this.SaveBucketData(EmailBucketName, Guid.NewGuid().ToString(), rawData);

        private async Task SaveBucketData(string bucketName, string objectKey, string rawData) =>
            await this.s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                ContentBody = rawData
            });
    }
}