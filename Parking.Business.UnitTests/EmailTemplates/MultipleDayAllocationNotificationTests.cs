namespace Parking.Business.UnitTests.EmailTemplates
{
    using System.Collections.Generic;
    using Business.EmailTemplates;
    using Model;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class MultipleDayAllocationNotificationTests
    {
        [Theory]
        [InlineData("1@abc.com")]
        [InlineData("2@xyz.co.uk")]
        public static void To_returns_email_address_of_corresponding_user(string emailAddress)
        {
            var template = new MultipleDayAllocationNotification(
                new List<Request>(),
                CreateUser.With(userId: "user1", emailAddress: emailAddress));

            Assert.Equal(emailAddress, template.To);
        }

        [Fact]
        public static void Subject_contains_expected_text()
        {
            const string ExpectedSubjectText = "Parking spaces allocated for multiple upcoming dates";
            
            var template = new MultipleDayAllocationNotification(
                new List<Request>(),
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"));

            Assert.Equal(ExpectedSubjectText, template.Subject);
        }

        [Fact]
        public static void Body_contains_request_dates()
        {
            var requests = new[]
            {
                new Request("user1", 21.December(2020), RequestStatus.Allocated),
                new Request("user1", 22.December(2020), RequestStatus.Requested),
                new Request("user1", 24.December(2020), RequestStatus.Requested),
            };

            var template = new MultipleDayAllocationNotification(
                requests,
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"));

            const string ExpectedPlainTextBody =
                "You have been allocated parking spaces for the following dates:\r\n\r\n" +
                "Mon 21 Dec\r\n" +
                "Tue 22 Dec\r\n" +
                "Thu 24 Dec\r\n\r\n" +
                "If there are spaces you no longer need, please cancel the corresponding requests so that they can be given to someone else.";
            const string ExpectedHtmlBody =
                "<p>You have been allocated parking spaces for the following dates:</p>\r\n" +
                "<ul>\r\n" +
                "<li>Mon 21 Dec</li>\r\n" +
                "<li>Tue 22 Dec</li>\r\n" +
                "<li>Thu 24 Dec</li>\r\n" +
                "</ul>\r\n" +
                "<p>If there are spaces you no longer need, please cancel the corresponding requests so that they can be given to someone else.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
        }
    }
}