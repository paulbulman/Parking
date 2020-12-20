namespace ParkingService.Data.UnitTests
{
    using System.Threading.Tasks;
    using Model;
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
                new Email("someone@example.com", "Test subject", "Test plain text body", "Test HTML body"));

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