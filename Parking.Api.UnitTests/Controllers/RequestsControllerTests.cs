namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Business;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;
    using static ControllerHelpers;

    public static class RequestsControllerTests
    {
        [Fact]
        public static async Task Returns_requests_from_repository_for_active_date_range()
        {
            var activeDates = new[]
            {
                2.February(2021),
                3.February(2021),
                4.February(2021)
            };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(c => c.GetActiveDates())
                .Returns(activeDates);

            var requests = new[]
            {
                new Request("user1", 2.February(2021), RequestStatus.Allocated),
                new Request("user2", 3.February(2021), RequestStatus.Allocated),
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(2.February(2021), 4.February(2021)))
                .ReturnsAsync(requests);

            var controller = new RequestsController(
                mockDateCalculator.Object,
                mockRequestRepository.Object);

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<IEnumerable<Request>>(result);

            Assert.Equal(requests, resultValue);
        }
    }
}