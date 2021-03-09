namespace Parking.Api.IntegrationTests
{
    using System.Threading;
    using Amazon;
    using Amazon.CognitoIdentityProvider;
    using Amazon.CognitoIdentityProvider.Model;
    using Amazon.DynamoDBv2;
    using Amazon.Runtime;
    using Amazon.SimpleEmail;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NodaTime;
    using NodaTime.Testing;

    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");
            var region = RegionEndpoint.EUWest2;

            var mockAmazonCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
            mockAmazonCognitoIdentityProvider
                .Setup(p => p.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AdminCreateUserResponse { User = new UserType { Username = "NewUserId" } });

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IClock>(new FakeClock(Instant.FromUtc(2021, 3, 1, 0, 0)));

                services.AddScoped(provider => mockAmazonCognitoIdentityProvider.Object);
                services.AddScoped<IAmazonDynamoDB>(provider => DatabaseHelpers.CreateClient());
                services.AddScoped(provider => NotificationHelpers.CreateClient());
                services.AddScoped(provider => StorageHelpers.CreateClient());

                services.AddScoped<IAmazonSimpleEmailService>(
                    provider => new AmazonSimpleEmailServiceClient(credentials, region));
            });
        }
    }
}