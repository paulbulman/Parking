namespace Parking.Business.UnitTests.EmailTemplates;

using Business.EmailTemplates;
using NodaTime;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;

public static class ReservationReminderTests
{
    [Theory]
    [InlineData("1@abc.com")]
    [InlineData("2@xyz.co.uk")]
    public static void To_returns_email_address_of_corresponding_user(string emailAddress)
    {
        var template = new ReservationReminder(
            CreateUser.With(userId: "user1", emailAddress: emailAddress),
            21.December(2020));

        Assert.Equal(emailAddress, template.To);
    }

    [Theory]
    [InlineData(11, 30, "No parking reservations entered for Mon 30 Nov")]
    [InlineData(1, 1, "No parking reservations entered for Wed 01 Jan")]
    public static void Subject_contains_requests_date(
        int month,
        int day,
        string expectedSubject)
    {
        var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

        var template = new ReservationReminder(
            user,
            new LocalDate(2020, month, day));

        Assert.Equal(expectedSubject, template.Subject);
    }

    [Fact]
    public static void Body_contains_requests_date()
    {
        var template = new ReservationReminder(
            CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
            21.December(2020));

        const string ExpectedPlainTextBody =
            "No reservations have yet been entered for Mon 21 Dec.\r\n\r\n" +
            "If no spaces need reserving for this date then you can ignore this message.\r\n\r\n" +
            "Otherwise, you should enter reservations by 11am to ensure spaces are allocated accordingly.\r\n\r\n" +
            "If you do not want to receive these emails, you can turn them off from your profile page in the app.";
        const string ExpectedHtmlTextBody =
            "<p>No reservations have yet been entered for Mon 21 Dec.</p>\r\n" +
            "<p>If no spaces need reserving for this date then you can ignore this message.</p>\r\n" +
            "<p>Otherwise, you should enter reservations by 11am to ensure spaces are allocated accordingly.</p>\r\n" +
            "<p>If you do not want to receive these emails, you can turn them off from your profile page in the app.</p>";

        Assert.Equal(ExpectedPlainTextBody, template.PlainTextBody);
        Assert.Equal(ExpectedHtmlTextBody, template.HtmlBody);
    }
}