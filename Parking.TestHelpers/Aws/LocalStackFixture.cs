namespace Parking.TestHelpers.Aws
{
    using System;
    using System.Threading.Tasks;
    using Testcontainers.LocalStack;
    using Xunit;

    public class LocalStackFixture : IAsyncLifetime
    {
        public static string ServiceUrl { get; private set; } = string.Empty;

        private readonly LocalStackContainer container = new LocalStackBuilder("localstack/localstack")
            .WithEnvironment("LOCALSTACK_AUTH_TOKEN", Environment.GetEnvironmentVariable("LOCALSTACK_AUTH_TOKEN") ?? string.Empty)
            .Build();

        public async ValueTask InitializeAsync()
        {
            await this.container.StartAsync();

            ServiceUrl = this.container.GetConnectionString();

            Environment.SetEnvironmentVariable("TABLE_NAME", "parking");
            Environment.SetEnvironmentVariable("FROM_EMAIL_ADDRESS", "test@example.com");
            Environment.SetEnvironmentVariable("TOPIC_NAME", "arn:aws:sns:eu-west-2:000000000000:parking-notifications");
            Environment.SetEnvironmentVariable("CORS_ORIGIN", "http://localhost");
            Environment.SetEnvironmentVariable("USER_POOL_ID", "eu-west-2_TestPool");
        }

        public async ValueTask DisposeAsync()
        {
            await this.container.DisposeAsync();
        }
    }
}
