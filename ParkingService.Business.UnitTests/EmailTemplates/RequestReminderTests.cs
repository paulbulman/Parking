namespace ParkingService.Business.UnitTests.EmailTemplates
{
    using Business.EmailTemplates;
    using Model;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class RequestReminderTests
    {
        [Theory]
        [InlineData("1@abc.com")]
        [InlineData("2@xyz.co.uk")]
        public static void To_returns_email_address_of_corresponding_user(string emailAddress)
        {
            var template = new RequestReminder(
                new User("user1", null, emailAddress),
                new DateInterval(21.December(2020), 24.December(2020)));

            Assert.Equal(emailAddress, template.To);
        }

        [Theory]
        [InlineData(11, 30, 12, 4, "No parking requests entered for Mon 30 Nov - Fri 04 Dec")]
        [InlineData(1, 1, 1, 1, "No parking requests entered for Wed 01 Jan - Wed 01 Jan")]
        public static void Subject_contains_requests_date_range(
            int firstMonth,
            int firstDay,
            int lastMonth,
            int lastDay,
            string expectedSubject)
        {
            var user = new User("user1", null, "1@abc.com");

            var template = new RequestReminder(
                user,
                new DateInterval(
                    new LocalDate(2020, firstMonth, firstDay),
                    new LocalDate(2020, lastMonth, lastDay)));

            Assert.Equal(expectedSubject, template.Subject);
        }

        [Fact]
        public static void Body_contains_requests_date_range()
        {
            var template = new RequestReminder(
                new User("user1", null, "1@abc.com"),
                new DateInterval(21.December(2020), 24.December(2020)));

            const string ExpectedPlainTextBody =
                "No requests have yet been entered for Mon 21 Dec - Thu 24 Dec.\r\n\r\n" +
                "If you do not need parking during this period you can ignore this message.\r\n\r\n" +
                "Otherwise, you should enter requests by the end of today to have them taken into account.";
            const string ExpectedHtmlTextBody =
                "<p>No requests have yet been entered for Mon 21 Dec - Thu 24 Dec.</p>\r\n" +
                "<p>If you do not need parking during this period you can ignore this message.</p>\r\n" +
                "<p>Otherwise, you should enter requests by the end of today to have them taken into account.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlTextBody, template.HtmlBody);
        }
    }
}