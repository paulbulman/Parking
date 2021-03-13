namespace Parking.Api.IntegrationTests
{
    using System.Threading;
    using Amazon.CognitoIdentityProvider;
    using Amazon.CognitoIdentityProvider.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using TestHelpers.Aws;

    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var mockAmazonCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
            mockAmazonCognitoIdentityProvider
                .Setup(p => p.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AdminCreateUserResponse { User = new UserType { Username = "NewUserId" } });

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(provider => mockAmazonCognitoIdentityProvider.Object);

                ConfigurationHelpers.ConfigureExternalServices(services);
            });
        }
    }
}