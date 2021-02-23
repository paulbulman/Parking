namespace Parking.Data.UnitTests
{
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    public static class NotificationRepositoryTests
    {
        [Fact]
        public static async Task Passes_notification_data_to_raw_item_repository()
        {
            const string Subject = "Test subject";
            const string Body = "Test body";

            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var notificationRepository = new NotificationRepository(mockRawItemRepository.Object);

            await notificationRepository.Send(Subject, Body);

            mockRawItemRepository.Verify(r => r.SendNotification(Subject, Body), Times.Once);
        }
    }
}