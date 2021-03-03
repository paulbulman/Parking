namespace Parking.Data
{
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;

    public class NotificationRepository : INotificationRepository
    {
        private readonly INotificationProvider notificationProvider;

        public NotificationRepository(INotificationProvider notificationProvider) =>
            this.notificationProvider = notificationProvider;

        public async Task Send(string subject, string body) =>
            await this.notificationProvider.SendNotification(subject, body);
    }
}