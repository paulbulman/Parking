namespace Parking.Business.UnitTests.EmailTemplates
{
    using Business.EmailTemplates;
    using Model;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class SingleDayAllocationNotificationTests
    {
        [Theory]
        [InlineData("1@abc.com")]
        [InlineData("2@xyz.co.uk")]
        public static void To_returns_email_address_of_corresponding_user(string emailAddress)
        {
            var template = new SingleDayAllocationNotification(
                new Request("user1", 2.January(2021), RequestStatus.Allocated),
                CreateUser.With(userId: "user1", emailAddress: emailAddress));

            Assert.Equal(emailAddress, template.To);
        }

        [Theory]
        [InlineData(3, 2, "Parking space allocated for Mon 02 Mar")]
        [InlineData(4, 1, "Parking space allocated for Wed 01 Apr")]
        public static void Subject_contains_request_date(int month, int day, string expectedSubject)
        {
            var template = new SingleDayAllocationNotification(
                new Request("user1", new LocalDate(2020, month, day), RequestStatus.Allocated),
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"));

            Assert.Equal(expectedSubject, template.Subject);
        }

        [Theory]
        [InlineData(3, 2, "Mon 02 Mar")]
        [InlineData(4, 1, "Wed 01 Apr")]
        public static void Body_contains_request_date(int month, int day, string expectedDateText)
        {
            var template = new SingleDayAllocationNotification(
                new Request("user1", new LocalDate(2020, month, day), RequestStatus.Allocated),
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"));

            var expectedPlainTextBody =
                $"You have been allocated a parking space for {expectedDateText}.\r\n\r\n" +
                "If you no longer need this space, please cancel your request so that it can be given to someone else.";
            var expectedHtmlBody =
                $"<p>You have been allocated a parking space for {expectedDateText}.</p>\r\n" +
                "<p>If you no longer need this space, please cancel your request so that it can be given to someone else.</p>";

            Assert.Equal(expectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(expectedHtmlBody, template.HtmlBody);
        }
    }
}