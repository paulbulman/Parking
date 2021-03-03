namespace Parking.Data.UnitTests
{
    using System.Threading.Tasks;
    using Aws;
    using Business.EmailTemplates;
    using Moq;
    using Xunit;

    public static class EmailRepositoryTests
    {
        [Fact]
        public static async Task Converts_email_to_raw_data()
        {
            var mockStorageProvider = new Mock<IStorageProvider>();

            var emailRepository = new EmailRepository(mockStorageProvider.Object, Mock.Of<IEmailSender>());

            await emailRepository.Send(
                Mock.Of<IEmailTemplate>(e =>
                    e.To == "someone@example.com" &&
                    e.Subject == "Test subject" &&
                    e.PlainTextBody == "Test plain text body" &&
                    e.HtmlBody == "Test HTML body"));

            const string ExpectedJson =
                "{" +
                "\"To\":\"someone@example.com\"," +
                "\"Subject\":\"Test subject\"," +
                "\"PlainTextBody\":\"Test plain text body\"," +
                "\"HtmlBody\":\"Test HTML body\"" +
                "}";

            mockStorageProvider.Verify(p => p.SaveEmail(ExpectedJson), Times.Once);
            mockStorageProvider.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Sends_email()
        {
            var mockEmailSender = new Mock<IEmailSender>();

            var emailRepository = new EmailRepository(Mock.Of<IStorageProvider>(), mockEmailSender.Object);

            var emailTemplate = Mock.Of<IEmailTemplate>();

            await emailRepository.Send(emailTemplate);

            mockEmailSender.Verify(s => s.Send(emailTemplate), Times.Once);
            mockEmailSender.VerifyNoOtherCalls();
        }
    }
}