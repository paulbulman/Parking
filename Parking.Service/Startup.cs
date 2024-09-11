namespace Parking.Service;

using System;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;
using Business;
using Business.Data;
using Business.ScheduledTasks;
using Data;
using Data.Aws;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Serilog;
using Serilog.Formatting.Compact;

public class Startup
{
    public ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        this.ConfigureExternalServices(services);

        services.AddLogging(builder =>
            builder.AddSerilog(
                new LoggerConfiguration()
                    .Destructure.ByTransforming<LocalDate>(d =>
                        LocalDatePattern.CreateWithCurrentCulture("yyyy-MM-dd").Format(d))
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new CompactJsonFormatter())
                    .CreateLogger(),
                dispose: true));

        services.AddScoped<IEmailProvider, EmailProvider>();
        services.AddScoped<IDatabaseProvider, DatabaseProvider>();
        services.AddScoped<IIdentityProvider, IdentityProvider>();
        services.AddScoped<INotificationProvider, NotificationProvider>();

        services.AddScoped<IAllocationCreator, AllocationCreator>();
        services.AddScoped<AllocationNotifier>();
        services.AddScoped<IBankHolidayRepository, BankHolidayRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IDateCalculator, DateCalculator>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<Random>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IRequestSorter, RequestSorter>();
        services.AddScoped<RequestUpdater>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ScheduledTaskRunner>();
        services.AddScoped<TriggerManager>();
        services.AddScoped<ITriggerRepository, TriggerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IScheduledTask, DailyNotification>();
        services.AddScoped<IScheduledTask, RequestReminder>();
        services.AddScoped<IScheduledTask, ReservationReminder>();
        services.AddScoped<IScheduledTask, SoftInterruptionUpdater>();
        services.AddScoped<IScheduledTask, WeeklyNotification>();

        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureExternalServices(IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);

        services.AddScoped<IAmazonCognitoIdentityProvider, AmazonCognitoIdentityProviderClient>();
        services.AddScoped<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddScoped<IAmazonSimpleEmailService>(_ => new AmazonSimpleEmailServiceClient(EmailProvider.Config));
        services.AddScoped<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>();
    }
}