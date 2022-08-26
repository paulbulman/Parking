namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Business;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public static class CreateRequestRepository
    {
        public static IRequestRepository WithRequests(
            IReadOnlyCollection<LocalDate> activeDates,
            IReadOnlyCollection<Request> requests)
        {
            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);

            mockRequestRepository
                .Setup(r => r.GetRequests(activeDates.ToDateInterval()))
                .ReturnsAsync(requests);

            return mockRequestRepository.Object;
        }

        public static IRequestRepository WithRequests(
            string userId,
            IReadOnlyCollection<LocalDate> activeDates,
            IReadOnlyCollection<Request> requests) =>
            MockWithRequests(userId, activeDates, requests).Object;

        public static IRequestRepository WithRequests(
            string userId,
            LocalDate localDate,
            IReadOnlyCollection<Request> requests) =>
            MockWithRequests(userId, new[] {localDate}, requests).Object;

        public static Mock<IRequestRepository> MockWithRequests(
            string userId,
            IReadOnlyCollection<LocalDate> activeDates,
            IReadOnlyCollection<Request> requests)
        {
            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);

            mockRequestRepository
                .Setup(r => r.GetRequests(userId, activeDates.ToDateInterval()))
                .ReturnsAsync(requests);

            return mockRequestRepository;
        }
    }
}