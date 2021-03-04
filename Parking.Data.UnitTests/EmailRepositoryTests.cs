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

            var emailRepository = new EmailRepository(Mock.Of<IEmailProvider>(), mockStorageProvider.Object);

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
            var mockEmailProvider = new Mock<IEmailProvider>();

            var emailRepository = new EmailRepository(mockEmailProvider.Object, Mock.Of<IStorageProvider>());

            var emailTemplate = Mock.Of<IEmailTemplate>();

            await emailRepository.Send(emailTemplate);

            mockEmailProvider.Verify(s => s.Send(emailTemplate), Times.Once);
            mockEmailProvider.VerifyNoOtherCalls();
        }
    }
}