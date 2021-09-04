namespace Parking.Service
{
    using System;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Business.ScheduledTasks;
    using Microsoft.Extensions.DependencyInjection;

    public static class TaskRunner
    {
        public static async Task RunTasksAsync(IServiceProvider provider)
        {
            var triggerManager = provider.GetRequiredService<TriggerManager>();

            try
            {
                if (await triggerManager.ShouldRun())
                {
                    var requestUpdater = provider.GetRequiredService<RequestUpdater>();
                    var updatedRequests = await requestUpdater.Update();

                    var allocationNotifier = provider.GetRequiredService<AllocationNotifier>();
                    await allocationNotifier.Notify(updatedRequests);

                    var scheduledTaskRunner = provider.GetRequiredService<ScheduledTaskRunner>();
                    await scheduledTaskRunner.RunScheduledTasks();
                }

                await triggerManager.MarkComplete();
            }
            catch (Exception initialException)
            {
                var notificationRepository = provider.GetRequiredService<INotificationRepository>();

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
    }
}