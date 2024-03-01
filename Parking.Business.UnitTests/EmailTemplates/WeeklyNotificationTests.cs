namespace Parking.Business.UnitTests.EmailTemplates;

using System.Collections.Generic;
using System.Linq;
using Business.EmailTemplates;
using Model;
using NodaTime;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;

public static class WeeklyNotificationTests
{
    [Theory]
    [InlineData("1@abc.com")]
    [InlineData("2@xyz.co.uk")]
    public static void To_returns_email_address_of_corresponding_user(string emailAddress)
    {
        var template = new WeeklyNotification(
            new List<Request>(),
            CreateUser.With(userId: "user1", emailAddress: emailAddress),
            new DateInterval(21.December(2020), 24.December(2020)).ToArray());

        Assert.Equal(emailAddress, template.To);
    }

    [Theory]
    [InlineData(11, 30, 12, 4, "Provisional parking status for Mon 30 Nov - Fri 04 Dec")]
    [InlineData(1, 1, 1, 1, "Provisional parking status for Wed 01 Jan - Wed 01 Jan")]
    public static void Subject_contains_requests_date_range(
        int firstMonth,
        int firstDay,
        int lastMonth,
        int lastDay,
        string expectedSubject)
    {
        var dateInterval = new DateInterval(
                new LocalDate(2020, firstMonth, firstDay),
                new LocalDate(2020, lastMonth, lastDay))
            .ToArray();

        var template = new WeeklyNotification(
            new List<Request>(),
            CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
            dateInterval);

        Assert.Equal(expectedSubject, template.Subject);
    }

    [Fact]
    public static void Body_contains_request_status_for_each_requested_date_in_period()
    {
        var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

        var requests = new[]
        {
            new Request(user.UserId, 21.December(2020), RequestStatus.Allocated),
            new Request(user.UserId, 22.December(2020), RequestStatus.Interrupted),
            new Request(user.UserId, 23.December(2020), RequestStatus.Interrupted),
            new Request(user.UserId, 24.December(2020), RequestStatus.Allocated)
        };

        var template = new WeeklyNotification(
            requests,
            user,
            new DateInterval(21.December(2020), 24.December(2020)).ToArray());

        const string ExpectedPlainTextBody =
            "You have been allocated parking spaces for the period Mon 21 Dec - Thu 24 Dec as follows:\r\n\r\n" +
            "Mon 21 Dec: Allocated\r\n" +
            "Tue 22 Dec: INTERRUPTED (0)\r\n" +
            "Wed 23 Dec: INTERRUPTED (0)\r\n" +
            "Thu 24 Dec: Allocated\r\n\r\n" +
            "The number in parentheses indicates how many other people are also waiting for a space on the given day.\r\n\r\n" +
            "Further spaces are released for each date on the preceding working day.";
        const string ExpectedHtmlBody =
            "<p>You have been allocated parking spaces for the period Mon 21 Dec - Thu 24 Dec as follows:</p>\r\n" +
            "<ul>\r\n" +
            "<li>Mon 21 Dec: Allocated</li>\r\n" +
            "<li>Tue 22 Dec: <strong>Interrupted</strong> (0)</li>\r\n" +
            "<li>Wed 23 Dec: <strong>Interrupted</strong> (0)</li>\r\n" +
            "<li>Thu 24 Dec: Allocated</li>\r\n" +
            "</ul>\r\n" +
            "<p>The number in parentheses indicates how many other people are also waiting for a space on the given day.</p>\r\n" +
            "<p>Further spaces are released for each date on the preceding working day.</p>";

        Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
        Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
    }

    [Fact]
    public static void Body_contains_other_interrupted_user_count_when_interrupted()
    {
        var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

        var requests = new[]
        {
            new Request(user.UserId, 21.December(2020), RequestStatus.Allocated),
            new Request("OTHER_REQUESTED", 21.December(2020), RequestStatus.Interrupted),
            new Request("OTHER_ALLOCATED", 21.December(2020), RequestStatus.Allocated),
            new Request(user.UserId, 22.December(2020), RequestStatus.Interrupted),
            new Request("OTHER_REQUESTED1", 22.December(2020), RequestStatus.Interrupted),
            new Request("OTHER_REQUESTED2", 22.December(2020), RequestStatus.Interrupted),
            new Request("OTHER_ALLOCATED", 22.December(2020), RequestStatus.Allocated),
            new Request(user.UserId, 23.December(2020), RequestStatus.Interrupted),
            new Request("OTHER_REQUESTED", 23.December(2020), RequestStatus.Interrupted),
            new Request("OTHER_ALLOCATED", 23.December(2020), RequestStatus.Allocated)
        };

        var template = new WeeklyNotification(
            requests,
            user,
            new DateInterval(21.December(2020), 23.December(2020)).ToArray());

        const string ExpectedPlainTextBody =
            "You have been allocated parking spaces for the period Mon 21 Dec - Wed 23 Dec as follows:\r\n\r\n" +
            "Mon 21 Dec: Allocated\r\n" +
            "Tue 22 Dec: INTERRUPTED (2)\r\n" +
            "Wed 23 Dec: INTERRUPTED (1)\r\n\r\n" +
            "The number in parentheses indicates how many other people are also waiting for a space on the given day.\r\n\r\n" +
            "Further spaces are released for each date on the preceding working day.";
        const string ExpectedHtmlBody =
            "<p>You have been allocated parking spaces for the period Mon 21 Dec - Wed 23 Dec as follows:</p>\r\n" +
            "<ul>\r\n" +
            "<li>Mon 21 Dec: Allocated</li>\r\n" +
            "<li>Tue 22 Dec: <strong>Interrupted</strong> (2)</li>\r\n" +
            "<li>Wed 23 Dec: <strong>Interrupted</strong> (1)</li>\r\n" +
            "</ul>\r\n" +
            "<p>The number in parentheses indicates how many other people are also waiting for a space on the given day.</p>\r\n" +
            "<p>Further spaces are released for each date on the preceding working day.</p>";

        Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
        Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
    }

    [Fact]
    public static void Body_does_not_contain_dates_in_period_with_no_request()
    {
        var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

        var requests = new[] { new Request(user.UserId, 21.December(2020), RequestStatus.Allocated) };

        var template = new WeeklyNotification(
            requests,
            user,
            new DateInterval(21.December(2020), 22.December(2020)).ToArray());

        const string ExpectedPlainTextBody =
            "You have been allocated parking spaces for the period Mon 21 Dec - Tue 22 Dec as follows:\r\n\r\n" +
            "Mon 21 Dec: Allocated";
        const string ExpectedHtmlBody =
            "<p>You have been allocated parking spaces for the period Mon 21 Dec - Tue 22 Dec as follows:</p>\r\n" +
            "<ul>\r\n<li>Mon 21 Dec: Allocated</li>\r\n</ul>";

        Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
        Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
    }

    [Fact]
    public static void Body_does_not_contain_dates_in_period_with_cancelled_request()
    {
        var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

        var requests = new[]
        {
            new Request(user.UserId, 21.December(2020), RequestStatus.Allocated),
            new Request(user.UserId, 22.December(2020), RequestStatus.Cancelled),
        };

        var template = new WeeklyNotification(
            requests,
            user,
            new DateInterval(21.December(2020), 22.December(2020)).ToArray());

        const string ExpectedPlainTextBody =
            "You have been allocated parking spaces for the period Mon 21 Dec - Tue 22 Dec as follows:\r\n\r\n" +
            "Mon 21 Dec: Allocated";
        const string ExpectedHtmlBody =
            "<p>You have been allocated parking spaces for the period Mon 21 Dec - Tue 22 Dec as follows:</p>\r\n" +
            "<ul>\r\n<li>Mon 21 Dec: Allocated</li>\r\n</ul>";

        Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
        Assert.Equal(ExpectedHtmlBody, template.HtmlBody);
    }
}