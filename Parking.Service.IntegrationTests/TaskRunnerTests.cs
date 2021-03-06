namespace Parking.Service.IntegrationTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
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
            await StorageHelpers.ResetStorage();

            await SetupConfiguration();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Allocates_requests()
        {
            await SetupSchedules();

            await StorageHelpers.CreateTrigger();

            await DatabaseHelpers.CreateUser(
                CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));
            await DatabaseHelpers.CreateRequests(
                "User1",
                "2021-03",
                new Dictionary<string, string> { { "01", "R" } });

            await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

            var savedRequests = await DatabaseHelpers.ReadRequests("User1", "2021-03");

            Assert.Equal(new[] {"01"}, savedRequests.Keys);
            Assert.Equal("A", savedRequests["01"]);

            await CheckSingleEmail("john.doe@example.com", "Parking space allocated for Mon 01 Mar");
        }

        [Fact]
        public async Task Sends_daily_summary_email()
        {
            await SetupSchedules(dailyNotificationDue: true);

            await StorageHelpers.CreateTrigger();

            await DatabaseHelpers.CreateUser(
                CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));

            // Current instant is defined such that the next daily summary date is 02-Mar-2021
            await DatabaseHelpers.CreateRequests(
                "User1",
                "2021-03",
                new Dictionary<string, string> { { "02", "R" } });

            await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

            await CheckSingleEmail("john.doe@example.com", "Parking status for Tue 02 Mar: Allocated");
        }

        [Fact]
        public async Task Sends_weekly_summary_email()
        {
            await SetupSchedules(weeklyNotificationDue: true);

            await StorageHelpers.CreateTrigger();

            await DatabaseHelpers.CreateUser(
                CreateUser.With(userId: "User1", emailAddress: "john.doe@example.com"));

            // Current instant is defined such that the next weekly summary date range is 08-Mar-2021 - 12-Mar-2021
            await DatabaseHelpers.CreateRequests(
                "User1",
                "2021-03",
                new Dictionary<string, string> { { "08", "R" }, { "12", "R" } });

            await TaskRunner.RunTasksAsync(CustomProviderFactory.CreateServiceProvider());

            await CheckSingleEmail("john.doe@example.com", "Provisional parking status for Mon 08 Mar - Fri 12 Mar");
        }

        private static async Task CheckSingleEmail(string expectedTo, string expectedSubject)
        {
            var savedEmails = await StorageHelpers.GetSavedEmails();

            Assert.Single(savedEmails);
            
            var rawSavedEmail = savedEmails.Single();

            var savedEmail = JsonSerializer.Deserialize<EmailTemplate>(rawSavedEmail);
            
            Assert.NotNull(savedEmail);

            Assert.Equal(expectedTo, savedEmail.To);
            Assert.Equal(expectedSubject, savedEmail.Subject);
        }

        private static async Task SetupConfiguration()
        {
            const string RawConfigurationData = 
                "{" +
                "\"NearbyDistance\":3.5," +
                "\"ShortLeadTimeSpaces\":2," +
                "\"TotalSpaces\":9" +
                "}";

            await StorageHelpers.SaveConfiguration(RawConfigurationData);
        }

        private static async Task SetupSchedules(
            bool dailyNotificationDue = false,
            bool requestReminderDue = false,
            bool reservationReminderDue = false,
            bool weeklyNotificationDue = false)
        {
            const string DueTime = "2020-01-01T00:00:00Z";
            const string NotDueTime = "2030-01-01T00:00:00Z";

            var dailyNotificationTime = dailyNotificationDue ? DueTime : NotDueTime;
            var requestReminderTime = requestReminderDue ? DueTime : NotDueTime;
            var reservationReminderTime = reservationReminderDue ? DueTime : NotDueTime;
            var weeklyNotificationTime = weeklyNotificationDue ? DueTime : NotDueTime;

            var rawScheduleData =
                "{" +
                $"\"DAILY_NOTIFICATION\":\"{dailyNotificationTime}\"," +
                $"\"REQUEST_REMINDER\":\"{requestReminderTime}\"," +
                $"\"RESERVATION_REMINDER\":\"{reservationReminderTime}\"," +
                $"\"WEEKLY_NOTIFICATION\":\"{weeklyNotificationTime}\"" +
                "}";

            await StorageHelpers.SaveSchedules(rawScheduleData);
        }

        private class EmailTemplate
        {
            public EmailTemplate(string to, string subject, string plainTextBody, string htmlBody)
            {
                this.To = to;
                this.Subject = subject;
                this.PlainTextBody = plainTextBody;
                this.HtmlBody = htmlBody;
            }

            public string To { get; }

            public string Subject { get; }

            public string PlainTextBody { get; }

            public string HtmlBody { get; }
        }
    }
}
