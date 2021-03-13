namespace Parking.Service.IntegrationTests
{
    using System;
    using Amazon.CognitoIdentityProvider;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using TestHelpers.Aws;

    public static class CustomProviderFactory
    {
        public static IServiceProvider CreateServiceProvider() => new CustomStartup().BuildServiceProvider();

        private class CustomStartup : Startup
        {
            protected override void ConfigureExternalServices(IServiceCollection services)
            {
                services.AddScoped(provider => Mock.Of<IAmazonCognitoIdentityProvider>());

                ConfigurationHelpers.ConfigureExternalServices(services);
            }
        }
    }
}