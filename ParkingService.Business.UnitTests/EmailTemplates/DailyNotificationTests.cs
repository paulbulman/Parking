namespace ParkingService.Business.UnitTests.EmailTemplates
{
    using System.Collections.Generic;
    using Business.EmailTemplates;
    using Model;
    using NodaTime;
    using Xunit;

    public static class DailyNotificationTests
    {
        [Theory]
        [InlineData("1@abc.com")]
        [InlineData("2@xyz.co.uk")]
        public static void To_returns_email_address_of_corresponding_user(string emailAddress)
        {
            var template = new DailyNotification(new List<Request>(), new User("user1", null, emailAddress));
            
            Assert.Equal(emailAddress, template.To);
        }

        [Theory]
        [InlineData(3, 2, "Parking status for Mon 02 Mar: allocated")]
        [InlineData(4, 1, "Parking status for Wed 01 Apr: allocated")]
        public static void Subject_contains_requests_date(int month, int day, string expectedSubject)
        {
            var user = new User("user1", null, "1@abc.com");

            var requests = new[]
            {
                new Request(user.UserId, new LocalDate(2020, month, day), RequestStatus.Allocated)
            };

            var template = new DailyNotification(requests, user);

            Assert.Equal(expectedSubject, template.Subject);
        }

        [Theory]
        [InlineData(RequestStatus.Allocated, "Parking status for Wed 01 Apr: allocated")]
        [InlineData(RequestStatus.Requested, "Parking status for Wed 01 Apr: INTERRUPTED")]
        public static void Subject_contains_request_status(RequestStatus requestStatus, string expectedSubject)
        {
            var user = new User("user1", null, "1@abc.com");

            var requests = new[]
            {
                new Request(user.UserId, new LocalDate(2020, 4, 1), requestStatus)
            };

            var template = new DailyNotification(requests, user);

            Assert.Equal(expectedSubject, template.Subject);
        }

        [Fact]
        public static void Body_contains_request_status_when_allocated()
        {
            var user = new User("user1", null, "1@abc.com");

            var requests = new[]
            {
                new Request(user.UserId, new LocalDate(2020, 4, 1), RequestStatus.Allocated)
            };

            var template = new DailyNotification(requests, user);

            const string ExpectedPlainTextBody =
                "You have been allocated a parking space for Wed 01 Apr.\r\n\r\n" +
                "If you no longer need this space, please cancel your request so that it can be given to someone else.";
            const string ExpectedHtmlBody =
                "<p>You have been allocated a parking space for Wed 01 Apr.</p>\r\n" +
                "<p>If you no longer need this space, please cancel your request so that it can be given to someone else.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
        }

        [Fact]
        public static void Body_contains_request_status_when_interrupted()
        {
            var user = new User("user1", null, "1@abc.com");

            var requests = new[]
            {
                new Request(user.UserId, new LocalDate(2020, 4, 1), RequestStatus.Requested)
            };

            var template = new DailyNotification(requests, user);

            const string ExpectedPlainTextBody =
                "You have NOT been allocated a parking space for Wed 01 Apr.\r\n\r\n" +
                "If someone else cancels their request you may be allocated one later, but otherwise you can NOT park at the office.\r\n\r\n" +
                "There are currently 0 other people also waiting for a space.";
            const string ExpectedHtmlBody =
                "<p>You have <strong>not</strong> been allocated a parking space for Wed 01 Apr.</p>\r\n" +
                "<p>If someone else cancels their request you may be allocated one later, but otherwise you can <strong>not</strong> park at the office.</p>\r\n" +
                "<p>There are currently 0 other people also waiting for a space.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
        }

        [Fact]
        public static void Body_contains_other_interrupted_user_count_when_interrupted()
        {
            var user = new User("user1", null, "1@abc.com");

            var requests = new[]
            {
                new Request(user.UserId, new LocalDate(2020, 4, 1), RequestStatus.Requested),
                new Request("user2", new LocalDate(2020, 4, 1), RequestStatus.Requested),
                new Request("user3", new LocalDate(2020, 4, 1), RequestStatus.Allocated),
                new Request("user4", new LocalDate(2020, 4, 1), RequestStatus.Cancelled)
            };

            var template = new DailyNotification(requests, user);

            const string ExpectedPlainTextBody =
                "You have NOT been allocated a parking space for Wed 01 Apr.\r\n\r\n" +
                "If someone else cancels their request you may be allocated one later, but otherwise you can NOT park at the office.\r\n\r\n" +
                "There is currently 1 other person also waiting for a space.";
            const string ExpectedHtmlBody =
                "<p>You have <strong>not</strong> been allocated a parking space for Wed 01 Apr.</p>\r\n" +
                "<p>If someone else cancels their request you may be allocated one later, but otherwise you can <strong>not</strong> park at the office.</p>\r\n" +
                "<p>There is currently 1 other person also waiting for a space.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
        }
    }
}