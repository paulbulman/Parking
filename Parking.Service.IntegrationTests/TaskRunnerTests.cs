namespace Parking.Service.IntegrationTests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestHelpers;
using TestHelpers.Aws;
using Xunit;

public class TaskRunnerTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await DatabaseHelpers.ResetDatabase();
        await EmailHelpers.ResetEmail();

        await SetupConfiguration();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PreProcesses_requests()
    {
        await SetupSchedules();

        await DatabaseHelpers.CreateConfiguration(new Dictionary<string, string>
        {
            {"nearbyDistance", "3.5"},
            {"shortLeadTimeSpaces", "0"},
            {"totalSpaces", "0"}
        });

        await DatabaseHelpers.CreateTrigger();

        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));

        await DatabaseHelpers.CreateRequests(
            "User1",
            "2021-03",
            new Dictionary<string, string> { { "02", "P" } });

        await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

        var savedRequests = await DatabaseHelpers.ReadRequests("User1", "2021-03");

        Assert.Equal(["02"], savedRequests.Keys);
        Assert.Equal("I", savedRequests["02"]);
    }

    [Fact]
    public async Task Allocates_requests()
    {
        await SetupSchedules();

        await DatabaseHelpers.CreateTrigger();

        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));
        await DatabaseHelpers.CreateRequests(
            "User1",
            "2021-03",
            new Dictionary<string, string> { { "01", "I" } });

        await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

        var savedRequests = await DatabaseHelpers.ReadRequests("User1", "2021-03");

        Assert.Equal(["01"], savedRequests.Keys);
        Assert.Equal("A", savedRequests["01"]);

        await CheckSingleEmail("john.doe@example.com", "Parking space allocated for Mon 01 Mar");
    }

    [Fact]
    public async Task Sends_daily_summary_email()
    {
        await SetupSchedules(dailyNotificationDue: true);

        await DatabaseHelpers.CreateTrigger();

        await DatabaseHelpers.CreateConfiguration(new Dictionary<string, string>
        {
            {"nearbyDistance", "3.5"},
            {"shortLeadTimeSpaces", "0"},
            {"totalSpaces", "0"}
        });

        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));

        // Current instant is defined such that the next daily summary date is 02-Mar-2021
        await DatabaseHelpers.CreateRequests(
            "User1",
            "2021-03",
            new Dictionary<string, string> { { "02", "I" } });

        await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

        await CheckSingleEmail("john.doe@example.com", "Parking status for Tue 02 Mar: INTERRUPTED");
    }

    [Fact]
    public async Task Updates_soft_interruption_status()
    {
        await SetupSchedules(softInterruptionUpdaterDue: true);

        await DatabaseHelpers.CreateConfiguration(new Dictionary<string, string>
        {
            {"nearbyDistance", "3.5"},
            {"shortLeadTimeSpaces", "0"},
            {"totalSpaces", "0"}
        });

        await DatabaseHelpers.CreateTrigger();

        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));

        // Current instant is defined such that the next daily summary date is 02-Mar-2021
        await DatabaseHelpers.CreateRequests(
            "User1",
            "2021-03",
            new Dictionary<string, string> { { "02", "P" } });

        await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

        var savedRequests = await DatabaseHelpers.ReadRequests("User1", "2021-03");

        Assert.Equal(["02"], savedRequests.Keys);
        Assert.Equal("S", savedRequests["02"]);
    }

    [Fact]
    public async Task Sends_weekly_summary_email()
    {
        await SetupSchedules(weeklyNotificationDue: true);

        await DatabaseHelpers.CreateTrigger();

        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));

        // Current instant is defined such that the next weekly summary date range is 08-Mar-2021 - 12-Mar-2021
        await DatabaseHelpers.CreateRequests(
            "User1",
            "2021-03",
            new Dictionary<string, string> { { "08", "I" }, { "12", "I" } });

        await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

        await CheckSingleEmail("john.doe@example.com", "Provisional parking status for Mon 08 Mar - Fri 12 Mar");
    }

    private static async Task CheckSingleEmail(string expectedTo, string expectedSubject)
    {
        var sentEmails = await EmailHelpers.GetSentEmails();

        Assert.Single(sentEmails);

        var sentEmail = sentEmails.Single();

        Assert.Single(sentEmail.Destination.ToAddresses);

        Assert.Equal(expectedTo, sentEmail.Destination.ToAddresses.Single());
        Assert.Equal(expectedSubject, sentEmail.Subject);
    }

    private static async Task SetupConfiguration() =>
        await DatabaseHelpers.CreateConfiguration(new Dictionary<string, string>
        {
            {"nearbyDistance", "3.5"},
            {"shortLeadTimeSpaces", "2"},
            {"totalSpaces", "9"}
        });

    private static async Task SetupSchedules(
        bool dailyNotificationDue = false,
        bool requestReminderDue = false,
        bool reservationReminderDue = false,
        bool softInterruptionUpdaterDue = false,
        bool weeklyNotificationDue = false)
    {
        const string DueTime = "2020-01-01T00:00:00Z";
        const string NotDueTime = "2030-01-01T00:00:00Z";

        var dailyNotificationTime = dailyNotificationDue ? DueTime : NotDueTime;
        var requestReminderTime = requestReminderDue ? DueTime : NotDueTime;
        var reservationReminderTime = reservationReminderDue ? DueTime : NotDueTime;
        var softInterruptionUpdaterTime = softInterruptionUpdaterDue ? DueTime : NotDueTime;
        var weeklyNotificationTime = weeklyNotificationDue ? DueTime : NotDueTime;

        var rawScheduleData = new Dictionary<string, string>
        {
            {"DAILY_NOTIFICATION", dailyNotificationTime},
            {"REQUEST_REMINDER", requestReminderTime},
            {"RESERVATION_REMINDER", reservationReminderTime},
            {"SOFT_INTERRUPTION_UPDATER", softInterruptionUpdaterTime},
            {"WEEKLY_NOTIFICATION", weeklyNotificationTime}
        };

        await DatabaseHelpers.CreateSchedules(rawScheduleData);
    }
}