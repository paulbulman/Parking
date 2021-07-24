namespace Parking.Business.UnitTests.EmailTemplates
{
    using System.Collections.Generic;
    using Business.EmailTemplates;
    using Model;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class DailyNotificationTests
    {
        [Theory]
        [InlineData("1@abc.com")]
        [InlineData("2@xyz.co.uk")]
        public static void To_returns_email_address_of_corresponding_user(string emailAddress)
        {
            var template = new DailyNotification(
                new List<Request>(),
                CreateUser.With(userId: "user1", emailAddress: emailAddress),
                22.December(2020));

            Assert.Equal(emailAddress, template.To);
        }

        [Theory]
        [InlineData(3, 2, "Parking status for Mon 02 Mar: Allocated")]
        [InlineData(4, 1, "Parking status for Wed 01 Apr: Allocated")]
        public static void Subject_contains_requests_date(int month, int day, string expectedSubject)
        {
            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");
            var localDate = new LocalDate(2020, month, day);

            var requests = new[] { new Request(user.UserId, localDate, RequestStatus.Allocated) };
            
            var template = new DailyNotification(
                requests,
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
                localDate);

            Assert.Equal(expectedSubject, template.Subject);
        }

        [Theory]
        [InlineData(RequestStatus.Allocated, "Parking status for Wed 01 Apr: Allocated")]
        [InlineData(RequestStatus.Requested, "Parking status for Wed 01 Apr: INTERRUPTED")]
        public static void Subject_contains_request_status(RequestStatus requestStatus, string expectedSubject)
        {
            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");
            var localDate = 1.April(2020);

            var requests = new[] { new Request(user.UserId, localDate, requestStatus) };

            var template = new DailyNotification(requests, user, localDate);

            Assert.Equal(expectedSubject, template.Subject);
        }

        [Fact]
        public static void Body_contains_request_status_when_allocated()
        {
            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");
            var localDate = 1.April(2020);

            var requests = new[] { new Request(user.UserId, localDate, RequestStatus.Allocated) };

            var template = new DailyNotification(requests, user, localDate);

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
            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");
            var localDate = 1.April(2020);

            var requests = new[] { new Request(user.UserId, localDate, RequestStatus.Requested) };

            var template = new DailyNotification(requests, user, localDate);

            const string ExpectedPlainTextBody =
                "You have NOT been allocated a parking space for Wed 01 Apr.\r\n\r\n" +
                "If someone else cancels their request you may be allocated one later, but otherwise you can NOT park at the office.\r\n\r\n" +
                "Alternatively, if you make other arrangements TO COME IN to the office, you can choose to stay interrupted via the Summary page within the app.\r\n\r\n" +
                "There are currently 0 other people also waiting for a space.";
            const string ExpectedHtmlBody =
                "<p>You have <strong>not</strong> been allocated a parking space for Wed 01 Apr.</p>\r\n" +
                "<p>If someone else cancels their request you may be allocated one later, but otherwise you can <strong>not</strong> park at the office.</p>\r\n" +
                "<p>Alternatively, if you make other arrangements <strong>to come in</strong> to the office, you can choose to stay interrupted via the Summary page within the app.</p>\r\n" +
                "<p>There are currently 0 other people also waiting for a space.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
        }

        [Fact]
        public static void Body_contains_other_interrupted_user_count_when_interrupted()
        {
            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");
            var localDate = 1.April(2020);

            var requests = new[]
            {
                new Request(user.UserId, localDate, RequestStatus.Requested),
                new Request("user2", localDate, RequestStatus.Requested),
                new Request("user3", localDate, RequestStatus.SoftInterrupted),
                new Request("user4", localDate, RequestStatus.Allocated),
                new Request("user5", localDate, RequestStatus.Cancelled),
            };

            var template = new DailyNotification(requests, user, localDate);

            const string ExpectedPlainTextBody =
                "You have NOT been allocated a parking space for Wed 01 Apr.\r\n\r\n" +
                "If someone else cancels their request you may be allocated one later, but otherwise you can NOT park at the office.\r\n\r\n" +
                "Alternatively, if you make other arrangements TO COME IN to the office, you can choose to stay interrupted via the Summary page within the app.\r\n\r\n" +
                "There are currently 2 other people also waiting for a space.";
            const string ExpectedHtmlBody =
                "<p>You have <strong>not</strong> been allocated a parking space for Wed 01 Apr.</p>\r\n" +
                "<p>If someone else cancels their request you may be allocated one later, but otherwise you can <strong>not</strong> park at the office.</p>\r\n" +
                "<p>Alternatively, if you make other arrangements <strong>to come in</strong> to the office, you can choose to stay interrupted via the Summary page within the app.</p>\r\n" +
                "<p>There are currently 2 other people also waiting for a space.</p>";

            Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
            Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
        }
    }
}