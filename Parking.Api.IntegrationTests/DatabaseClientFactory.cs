namespace Parking.Api.IntegrationTests
{
    using Amazon;
    using Amazon.DynamoDBv2;
    using Amazon.Runtime;

    public static class DatabaseClientFactory
    {
        public static AmazonDynamoDBClient Create()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.EUWest2,
                ServiceURL = "http://localhost:4566"
            };

            return new AmazonDynamoDBClient(credentials, config);
        }
    }
}