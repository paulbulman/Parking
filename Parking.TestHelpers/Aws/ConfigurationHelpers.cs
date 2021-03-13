namespace Parking.TestHelpers.Aws
{
    using Microsoft.Extensions.DependencyInjection;
    using NodaTime;
    using NodaTime.Testing;

    public static class ConfigurationHelpers
    {
        private static readonly Instant CurrentInstant = Instant.FromUtc(2021, 3, 1, 0, 0);

        public static void ConfigureExternalServices(IServiceCollection services)
        {
            services.AddSingleton<IClock>(new FakeClock(CurrentInstant));

            services.AddScoped(provider => DatabaseHelpers.CreateClient());
            services.AddScoped(provider => EmailHelpers.CreateClient());
            services.AddScoped(provider => NotificationHelpers.CreateClient());
            services.AddScoped(provider => StorageHelpers.CreateClient());
        }
    }
}