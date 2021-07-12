// ReSharper disable StringLiteralTypo
namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.DailyDetails;
    using Model;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class DailyDetailsControllerTests
    {
        [Fact]
        public static async Task Returns_details_data_for_each_active_date()
        {
            var activeDates = new[] {12.July(2021), 13.July(2021), 16.July(2021)};
            var longLeadTimeAllocationDates = new[] {16.July(2021)};

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new DailyDetailsController(
                dateCalculator,
                CreateRequestRepository.WithRequests(activeDates, new List<Request>()),
                CreateUserRepository.WithUsers(new List<User>()))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<DailyDetailsResponse>(result);

            Assert.Equal(activeDates, resultValue.Details.Select(d => d.LocalDate));
        }

        [Fact]
        public static async Task Groups_allocated_and_interrupted_users_sorted_by_last_name()
        {
            var activeDates = new[] {12.July(2021)};
            var longLeadTimeAllocationDates = new[] {16.July(2021)};

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
                CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
                CreateUser.With(userId: "user3", firstName: "Shannen", lastName: "Muddicliffe"),
                CreateUser.With(userId: "user4", firstName: "Marco", lastName: "Call"),
            };

            var requests = new[]
            {
                new Request("user1", 12.July(2021), RequestStatus.Allocated),
                new Request("user2", 12.July(2021), RequestStatus.Allocated),
                new Request("user3", 12.July(2021), RequestStatus.Requested),
                new Request("user4", 12.July(2021), RequestStatus.Requested),
            };

            var controller = new DailyDetailsController(
                dateCalculator,
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(users))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<DailyDetailsResponse>(result);

            var data = GetDailyData(resultValue.Details, 12.July(2021));

            Assert.Equal(new[] {"Hynda Lindback", "Cathie Phoenix"}, data.AllocatedUsers.Select(u => u.Name));
            Assert.Equal(new[] {"Marco Call", "Shannen Muddicliffe"}, data.InterruptedUsers.Select(u => u.Name));
            Assert.Empty(data.RequestedUsers);
        }

        [Fact]
        public static async Task Returns_status_as_requested_when_outside_long_lead_time()
        {
            var activeDates = new[] { 12.July(2021) };
            var longLeadTimeAllocationDates = new[] { 11.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var users = new[]
            {
                CreateUser.With(userId: "user3", firstName: "Shannen", lastName: "Muddicliffe"),
                CreateUser.With(userId: "user4", firstName: "Marco", lastName: "Call"),
            };

            var requests = new[]
            {
                new Request("user3", 12.July(2021), RequestStatus.Requested),
                new Request("user4", 12.July(2021), RequestStatus.Requested),
            };

            var controller = new DailyDetailsController(
                dateCalculator,
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(users))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<DailyDetailsResponse>(result);

            var data = GetDailyData(resultValue.Details, 12.July(2021));

            Assert.Empty(data.AllocatedUsers);
            Assert.Empty(data.InterruptedUsers);
            Assert.Equal(new[] { "Marco Call", "Shannen Muddicliffe" }, data.RequestedUsers.Select(u => u.Name));
        }

        [Fact]
        public static async Task Ignores_cancelled_requests()
        {
            var activeDates = new[] { 16.July(2021), 17.July(2021) };
            var longLeadTimeAllocationDates = new[] { 16.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
                CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
                CreateUser.With(userId: "user3", firstName: "Shannen", lastName: "Muddicliffe"),
            };

            var requests = new[]
            {
                new Request("user1", 16.July(2021), RequestStatus.Cancelled),
                new Request("user2", 16.July(2021), RequestStatus.Cancelled),
                new Request("user3", 17.July(2021), RequestStatus.Cancelled),
            };

            var controller = new DailyDetailsController(
                dateCalculator,
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(users))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<DailyDetailsResponse>(result);

            var day1Data = GetDailyData(resultValue.Details, 16.July(2021));
            var day2Data = GetDailyData(resultValue.Details, 17.July(2021));

            Assert.Empty(day1Data.AllocatedUsers);
            Assert.Empty(day1Data.InterruptedUsers);
            Assert.Empty(day2Data.RequestedUsers);
        }

        [Fact]
        public static async Task Highlights_active_user()
        {
            var activeDates = new[] { 16.July(2021), 17.July(2021), 18.July(2021) };
            var longLeadTimeAllocationDates = new[] { 17.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
                CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
            };

            var requests = new[]
            {
                new Request("user1", 16.July(2021), RequestStatus.Allocated),
                new Request("user2", 16.July(2021), RequestStatus.Requested),
                new Request("user1", 17.July(2021), RequestStatus.Requested),
                new Request("user2", 17.July(2021), RequestStatus.Allocated),
                new Request("user2", 18.July(2021), RequestStatus.Requested),
            };

            var controller = new DailyDetailsController(
                dateCalculator,
                CreateRequestRepository.WithRequests(activeDates, requests),
                CreateUserRepository.WithUsers(users))
            {
                ControllerContext = CreateControllerContext.WithUsername("user2")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<DailyDetailsResponse>(result);

            var day1Data = GetDailyData(resultValue.Details, 16.July(2021));
            var day2Data = GetDailyData(resultValue.Details, 17.July(2021));
            var day3Data = GetDailyData(resultValue.Details, 18.July(2021));

            Assert.False(day1Data.AllocatedUsers.Single().IsHighlighted);
            Assert.True(day1Data.InterruptedUsers.Single().IsHighlighted);

            Assert.True(day2Data.AllocatedUsers.Single().IsHighlighted);
            Assert.False(day2Data.InterruptedUsers.Single().IsHighlighted);

            Assert.True(day3Data.RequestedUsers.Single().IsHighlighted);
        }
    }
}