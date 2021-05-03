namespace Parking.Data.Aws
{
    using System.Threading.Tasks;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;

    public interface INotificationProvider
    {
        Task SendNotification(string subject, string body);
    }

    public class NotificationProvider : INotificationProvider
    {
        private readonly IAmazonSimpleNotificationService simpleNotificationService;

        public NotificationProvider(IAmazonSimpleNotificationService simpleNotificationService) =>
            this.simpleNotificationService = simpleNotificationService;

        private static string NotificationTopic => Helpers.GetRequiredEnvironmentVariable("TOPIC_NAME");

        public async Task SendNotification(string subject, string body) =>
            await this.simpleNotificationService.PublishAsync(new PublishRequest(NotificationTopic, body, subject));
    }
}