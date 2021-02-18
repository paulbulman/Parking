// ReSharper disable StringLiteralTypo
namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Calendar;
    using Api.Json.Overview;
    using Model;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class OverviewControllerTests
    {
        [Fact]
        public static async Task Returns_overview_data_for_each_active_date()
        {
            var activeDates = new[] { 15.February(2021), 16.February(2021), 18.February(2021) };

            var controller = new OverviewController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(activeDates, new List<Request>()),
                CreateUserRepository.WithUsers(new List<User>()));

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<OverviewData>>(result);

            var visibleDays = GetVisibleDays(calendar);

            Assert.Equal(activeDates, visibleDays.Select(d => d.LocalDate));

            Assert.All(visibleDays, d => Assert.NotNull(d.Data));
        }

        [Fact]
        public static async Task Groups_allocated_and_interrupted_users_sorted_by_last_name()
        {
            var activeDates = new[] { 15.February(2021) };

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
                CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
                CreateUser.With(userId: "user3", firstName: "Shannen", lastName: "Muddicliffe"),
                CreateUser.With(userId: "user4", firstName: "Marco", lastName: "Call"),
            };

            var requests = new[]
            {
                new Request("user1", 15.February(2021), RequestStatus.Allocated),
                new Request("user2", 15.February(2021), RequestStatus.Allocated),
                new Request("user3", 15.February(2021), RequestStatus.Requested),
                new Request("user4", 15.February(2021), RequestStatus.Requested),
            };

            var controller = new OverviewController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<OverviewData>>(result);

            var data = GetDailyData(calendar, 15.February(2021));

            Assert.Equal(new[] { "Hynda Lindback", "Cathie Phoenix" }, data.AllocatedUsers.Select(u => u.Name));
            Assert.Equal(new[] { "Marco Call", "Shannen Muddicliffe" }, data.InterruptedUsers.Select(u => u.Name));
        }

        [Fact]
        public static async Task Returns_empty_object_when_no_requests_exist()
        {
            var activeDates = new[] { 15.February(2021) };

            var controller = new OverviewController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(activeDates, new List<Request>()),
                CreateUserRepository.WithUsers(new List<User>()));

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<OverviewData>>(result);

            var data = GetDailyData(calendar, 15.February(2021));

            Assert.Empty(data.AllocatedUsers);
            Assert.Empty(data.InterruptedUsers);
        }

        [Fact]
        public static async Task Ignores_cancelled_requests()
        {
            var activeDates = new[] { 15.February(2021) };

            var requests = new[] { new Request("user1", 15.February(2021), RequestStatus.Cancelled) };

            var controller = new OverviewController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(new List<User>()));

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<OverviewData>>(result);

            var data = GetDailyData(calendar, 15.February(2021));

            Assert.Empty(data.AllocatedUsers);
            Assert.Empty(data.InterruptedUsers);
        }

        [Fact]
        public static async Task Highlights_active_user()
        {
            var activeDates = new[] {15.February(2021), 16.February(2021)};

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
                CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
            };

            var requests = new[]
            {
                new Request("user1", 15.February(2021), RequestStatus.Allocated),
                new Request("user2", 15.February(2021), RequestStatus.Requested),
                new Request("user1", 16.February(2021), RequestStatus.Requested),
                new Request("user2", 16.February(2021), RequestStatus.Allocated),
            };

            var controller = new OverviewController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(users))
            {
                ControllerContext = CreateControllerContext.WithUsername("user2")
            };

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<OverviewData>>(result);

            var day1Data = GetDailyData(calendar, 15.February(2021));
            var day2Data = GetDailyData(calendar, 16.February(2021));

            Assert.False(day1Data.AllocatedUsers.Single().IsHighlighted);
            Assert.True(day1Data.InterruptedUsers.Single().IsHighlighted);

            Assert.True(day2Data.AllocatedUsers.Single().IsHighlighted);
            Assert.False(day2Data.InterruptedUsers.Single().IsHighlighted);
        }
    }
}