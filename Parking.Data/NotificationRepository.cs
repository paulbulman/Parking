namespace Parking.Data
{
    using System.Threading.Tasks;
    using Business.Data;

    public class NotificationRepository : INotificationRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public NotificationRepository(IRawItemRepository rawItemRepository) =>
            this.rawItemRepository = rawItemRepository;

        public async Task Send(string subject, string body) =>
            await this.rawItemRepository.SendNotification(subject, body);
    }
}