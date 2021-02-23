namespace Parking.Service
{
    using System;
    using System.Threading.Tasks;
    using Amazon.CognitoIdentityProvider;
    using Amazon.DynamoDBv2;
    using Amazon.S3;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleSystemsManagement;
    using Business;
    using Business.Data;
    using Business.ScheduledTasks;
    using Data;
    using Microsoft.Extensions.DependencyInjection;
    using NodaTime;

    public class TaskRunner
    {
        private readonly ServiceProvider serviceProvider;

        public TaskRunner() => this.serviceProvider = BuildServiceProvider();

        public async Task RunTasksAsync()
        {
            using var scope = this.serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            var triggerManager = scope.ServiceProvider.GetRequiredService<TriggerManager>();

            try
            {
                if (await triggerManager.ShouldRun())
                {
                    var requestUpdater = scope.ServiceProvider.GetRequiredService<RequestUpdater>();
                    var allocatedRequests = await requestUpdater.Update();

                    var allocationNotifier = scope.ServiceProvider.GetRequiredService<AllocationNotifier>();
                    await allocationNotifier.Notify(allocatedRequests);

                    var scheduledTaskRunner = scope.ServiceProvider.GetRequiredService<ScheduledTaskRunner>();
                    await scheduledTaskRunner.RunScheduledTasks();
                }

                await triggerManager.MarkComplete();
            }
            catch (Exception initialException)
            {
                var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                try
                {
                    await notificationRepository.Send("Unhandled exception", initialException.ToString());
                }
                catch (Exception notificationException)
                {
                    Console.WriteLine(
                        $"Exception occurred attempting to send exception notification: {notificationException}");
                }

                throw;
            }
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IClock>(SystemClock.Instance);

            services.AddScoped<IAmazonCognitoIdentityProvider, AmazonCognitoIdentityProviderClient>();
            services.AddScoped<IAmazonDynamoDB, AmazonDynamoDBClient>();
            services.AddScoped<IAmazonS3, AmazonS3Client>();
            services.AddScoped<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>();
            services.AddScoped<IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementClient>();

            services.AddScoped<IAllocationCreator, AllocationCreator>();
            services.AddScoped<AllocationNotifier>();
            services.AddScoped<IBankHolidayRepository, BankHolidayRepository>();
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IDateCalculator, DateCalculator>();
            services.AddScoped<IEmailRepository, EmailRepository>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<Random>();
            services.AddScoped<IRawItemRepository, RawItemRepository>();
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
            services.AddScoped<IScheduledTask, WeeklyNotification>();

            return services.BuildServiceProvider();
        }
    }
}