namespace Parking.Data.UnitTests
{
    using System.Threading.Tasks;
    using Business.EmailTemplates;
    using Moq;
    using Xunit;

    public static class EmailRepositoryTests
    {
        [Fact]
        public static async Task Converts_email_to_raw_data()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>();

            var emailRepository = new EmailRepository(mockRawItemRepository.Object);

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

            mockRawItemRepository.Verify(r => r.SendEmail(ExpectedJson), Times.Once);
            mockRawItemRepository.VerifyNoOtherCalls();
        }
    }
}