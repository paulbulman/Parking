namespace Parking.Business.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class RequestPreProcessorTests
    {
        [Fact]
        public static async Task Updates_pending_requests_to_interrupted()
        {
            var mockDateCalculator = new Mock<IDateCalculator>();
            
            mockDateCalculator
                .Setup(c => c.GetShortLeadTimeAllocationDates())
                .Returns(new[] {26.July(2021), 27.July(2021)});
            mockDateCalculator
                .Setup(c => c.GetLongLeadTimeAllocationDates())
                .Returns(new[] {28.July(2021), 29.July(2021)});

            var initialRequests = new[]
            {
                new Request("user1", 26.July(2021), RequestStatus.Pending),
                new Request("user2", 26.July(2021), RequestStatus.Allocated),
                new Request("user1", 28.July(2021), RequestStatus.Interrupted),
                new Request("user2", 29.July(2021), RequestStatus.Pending)
            };

            var mockRequestRepository = new Mock<IRequestRepository>();

            mockRequestRepository
                .Setup(r => r.GetRequests(26.July(2021), 29.July(2021)))
                .ReturnsAsync(initialRequests);
            mockRequestRepository
                .Setup(r => r.SaveRequests(It.IsAny<IReadOnlyCollection<Request>>()))
                .Returns(Task.CompletedTask);

            var requestPreProcessor = new RequestPreProcessor(mockDateCalculator.Object, mockRequestRepository.Object);

            await requestPreProcessor.Update();

            mockDateCalculator.Verify();

            var expectedSavedRequests = new[]
            {
                new Request("user1", 26.July(2021), RequestStatus.Interrupted),
                new Request("user2", 29.July(2021), RequestStatus.Interrupted)
            };

            mockRequestRepository.Verify(
                r => r.SaveRequests(It.Is<IReadOnlyCollection<Request>>(
                    actual => CheckRequests(expectedSavedRequests, actual.ToList()))),
                Times.Once);
        }
        
        private static bool CheckRequests(
            IReadOnlyCollection<Request> expected,
            IReadOnlyCollection<Request> actual) =>
            actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RequestsComparer()));
    }
}