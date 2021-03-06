﻿namespace Parking.Api.IntegrationTests
{
    using Amazon;
    using Amazon.CognitoIdentityProvider;
    using Amazon.DynamoDBv2;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.SimpleEmail;
    using Amazon.SimpleNotificationService;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using NodaTime;
    using NodaTime.Testing;

    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");
            var region = RegionEndpoint.EUWest2;

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IClock>(new FakeClock(Instant.FromUtc(2021, 3, 1, 0, 0)));

                services.AddScoped<IAmazonCognitoIdentityProvider>(
                    provider => new AmazonCognitoIdentityProviderClient(credentials, region));
                services.AddScoped<IAmazonDynamoDB>(
                    provider => DatabaseClientFactory.Create());
                services.AddScoped<IAmazonS3>(
                    provider => new AmazonS3Client(credentials, region));
                services.AddScoped<IAmazonSimpleEmailService>(
                    provider => new AmazonSimpleEmailServiceClient(credentials, region));
                services.AddScoped<IAmazonSimpleNotificationService>(
                    provider => new AmazonSimpleNotificationServiceClient(credentials, region));
            });
        }
    }
}