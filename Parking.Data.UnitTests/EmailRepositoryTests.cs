namespace Parking.Data.UnitTests;

using System.Threading.Tasks;
using Aws;
using Business.EmailTemplates;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public static class EmailRepositoryTests
{
    [Fact]
    public static async Task Sends_email()
    {
        var mockEmailProvider = new Mock<IEmailProvider>();

        var emailRepository = new EmailRepository(
            Mock.Of<ILogger<EmailRepository>>(),
            mockEmailProvider.Object);

        var emailTemplate = Mock.Of<IEmailTemplate>();

        await emailRepository.Send(emailTemplate);

        mockEmailProvider.Verify(s => s.Send(emailTemplate), Times.Once);
        mockEmailProvider.VerifyNoOtherCalls();
    }
}