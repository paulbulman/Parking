namespace Parking.Data.UnitTests;

using System.Threading.Tasks;
using Aws;
using Moq;
using Xunit;

public static class NotificationRepositoryTests
{
    [Fact]
    public static async Task Passes_notification_data_to_notification_provider()
    {
        const string Subject = "Test subject";
        const string Body = "Test body";

        var mockNotificationProvider = new Mock<INotificationProvider>();

        var notificationRepository = new NotificationRepository(mockNotificationProvider.Object);

        await notificationRepository.Send(Subject, Body);

        mockNotificationProvider.Verify(p => p.SendNotification(Subject, Body), Times.Once);
    }
}