namespace Parking.Business.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class RequestUpdaterTests
    {
        private static readonly IReadOnlyCollection<User> DefaultUsers = new List<User>
        {
            CreateUser.With(userId: "user1", firstName: "User", lastName: "1"),
            CreateUser.With(userId: "user2", firstName: "User", lastName: "2"),
        };

        private static readonly IReadOnlyCollection<LocalDate> ShortLeadTimeDates = new[]
        {
            3.December(2020),
            4.December(2020)
        };

        private static readonly IReadOnlyCollection<LocalDate> LongLeadTimeDates = new[]
        {
            5.December(2020),
            6.December(2020)
        };

        private static readonly IReadOnlyCollection<LocalDate> AllocationDates = ShortLeadTimeDates
            .Concat(LongLeadTimeDates)
            .ToArray();

        private static readonly LocalDate EarliestConsideredDate = 4.October(2020);
        private static readonly LocalDate LastConsideredDate = 6.December(2020);

        private static readonly IReadOnlyCollection<Request> InitialRequests = new[]
        {
            new Request("user1", 1.December(2020), RequestStatus.Allocated),
            new Request("user2", 1.December(2020), RequestStatus.Allocated),
            new Request("user1", 2.December(2020), RequestStatus.Allocated),
            new Request("user2", 2.December(2020), RequestStatus.Allocated),
            new Request("user1", 3.December(2020), RequestStatus.Interrupted),
            new Request("user2", 3.December(2020), RequestStatus.Interrupted),
            new Request("user1", 4.December(2020), RequestStatus.Interrupted),
            new Request("user1", 5.December(2020), RequestStatus.Interrupted),
            new Request("user2", 5.December(2020), RequestStatus.Interrupted),
            new Request("user1", 6.December(2020), RequestStatus.Interrupted)
        };

        private static readonly IReadOnlyCollection<Request> NewlyAllocatedRequests = new[]
        {
            new Request("user1", 3.December(2020), RequestStatus.Allocated),
            new Request("user2", 3.December(2020), RequestStatus.Allocated),
            new Request("user1", 4.December(2020), RequestStatus.Allocated),
            new Request("user1", 5.December(2020), RequestStatus.Allocated),
            new Request("user2", 5.December(2020), RequestStatus.Allocated),
            new Request("user1", 6.December(2020), RequestStatus.Allocated)
        };

        [Fact]
        public static async Task Updates_requests_for_long_lead_time_and_short_lead_time()
        {
            var mockAllocationCreator = new Mock<IAllocationCreator>(MockBehavior.Strict);

            foreach (var date in AllocationDates)
            {
                var expectedLeadTimeType = LongLeadTimeDates.Contains(date) ? LeadTimeType.Long : LeadTimeType.Short;

                mockAllocationCreator
                    .Setup(a => a.Create(
                        date,
                        It.IsAny<IReadOnlyCollection<Request>>(),
                        It.IsAny<IReadOnlyCollection<Reservation>>(),
                        It.IsAny<IReadOnlyList<User>>(),
                        It.IsAny<Configuration>(),
                        expectedLeadTimeType))
                    .Returns(NewlyAllocatedRequests.Where(r => r.Date == date).ToArray());
            }

            var mockRequestRepository = CreateMockRequestRepository();

            var requestUpdater = new RequestUpdater(
                mockAllocationCreator.Object,
                Mock.Of<IConfigurationRepository>(),
                CreateMockDateCalculator().Object,
                mockRequestRepository.Object,
                Mock.Of<IReservationRepository>(),
                CreateUserRepository.WithUsers(DefaultUsers));

            await requestUpdater.Update();

            mockRequestRepository.VerifyAll();
        }

        [Fact]
        public static async Task Uses_newly_updated_requests_from_previous_days()
        {
            var mockAllocationCreator = new Mock<IAllocationCreator>(MockBehavior.Strict);

            foreach (var date in AllocationDates)
            {
                var expectedCumulativeRequests = InitialRequests.Where(r => r.Date < ShortLeadTimeDates.First())
                    .Concat(NewlyAllocatedRequests.Where(r => r.Date < date))
                    .Concat(InitialRequests.Where(r => r.Date >= date))
                    .ToArray();

                mockAllocationCreator
                    .Setup(a => a.Create(
                        date,
                        It.Is<IReadOnlyCollection<Request>>(r =>
                            r.Count == expectedCumulativeRequests.Length && expectedCumulativeRequests.All(r.Contains)),
                        It.IsAny<IReadOnlyCollection<Reservation>>(),
                        It.IsAny<IReadOnlyCollection<User>>(),
                        It.IsAny<Configuration>(),
                        It.IsAny<LeadTimeType>()))
                    .Returns(NewlyAllocatedRequests.Where(r => r.Date == date).ToArray());
            }

            var requestUpdater = new RequestUpdater(
                mockAllocationCreator.Object,
                Mock.Of<IConfigurationRepository>(),
                CreateMockDateCalculator().Object,
                CreateMockRequestRepository().Object,
                Mock.Of<IReservationRepository>(),
                CreateUserRepository.WithUsers(DefaultUsers));

            await requestUpdater.Update();

            mockAllocationCreator.VerifyAll();
        }

        [Fact]
        public static async Task Returns_newly_updated_requests()
        {
            var mockAllocationCreator = new Mock<IAllocationCreator>(MockBehavior.Strict);

            foreach (var date in AllocationDates)
            {
                mockAllocationCreator
                    .Setup(a => a.Create(
                        date,
                        It.IsAny<IReadOnlyCollection<Request>>(),
                        It.IsAny<IReadOnlyCollection<Reservation>>(),
                        It.IsAny<IReadOnlyCollection<User>>(),
                        It.IsAny<Configuration>(),
                        It.IsAny<LeadTimeType>()))
                    .Returns(NewlyAllocatedRequests.Where(r => r.Date == date).ToArray());
            }

            var requestUpdater = new RequestUpdater(
                mockAllocationCreator.Object,
                Mock.Of<IConfigurationRepository>(),
                CreateMockDateCalculator().Object,
                CreateMockRequestRepository().Object,
                Mock.Of<IReservationRepository>(),
                CreateUserRepository.WithUsers(DefaultUsers));

            var result = await requestUpdater.Update();

            Assert.NotNull(result);
            Assert.Equal(NewlyAllocatedRequests.Count, result.Count);
            Assert.All(NewlyAllocatedRequests, r => Assert.Contains(r, result));
        }

        [Fact]
        public static async Task Uses_repository_dependencies()
        {
            var arbitraryReservation = new Reservation("user1", 4.December(2020));
            var reservations = new[] { arbitraryReservation };

            var mockReservationRepository = new Mock<IReservationRepository>(MockBehavior.Strict);
            mockReservationRepository
                .Setup(r => r.GetReservations(EarliestConsideredDate, LastConsideredDate))
                .ReturnsAsync(reservations);

            var arbitraryUser = CreateUser.With(userId: "user1");
            var users = new[] { arbitraryUser };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var arbitraryConfiguration = new Configuration(1, 2, 3);

            var mockConfigurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            mockConfigurationRepository
                .Setup(r => r.GetConfiguration())
                .ReturnsAsync(arbitraryConfiguration);

            var mockAllocationCreator = new Mock<IAllocationCreator>(MockBehavior.Strict);

            foreach (var date in AllocationDates)
            {
                mockAllocationCreator
                    .Setup(a => a.Create(
                        date,
                        It.IsAny<IReadOnlyCollection<Request>>(),
                        reservations,
                        users,
                        arbitraryConfiguration,
                        It.IsAny<LeadTimeType>()))
                    .Returns(NewlyAllocatedRequests.Where(r => r.Date == date).ToArray());
            }

            var requestUpdater = new RequestUpdater(
                mockAllocationCreator.Object,
                mockConfigurationRepository.Object,
                CreateMockDateCalculator().Object,
                CreateMockRequestRepository().Object,
                mockReservationRepository.Object,
                mockUserRepository.Object);

            await requestUpdater.Update();

            mockAllocationCreator.VerifyAll();
        }

        private static Mock<IDateCalculator> CreateMockDateCalculator()
        {
            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);

            mockDateCalculator
                .Setup(d => d.GetShortLeadTimeAllocationDates())
                .Returns(ShortLeadTimeDates);
            mockDateCalculator
                .Setup(d => d.GetLongLeadTimeAllocationDates())
                .Returns(LongLeadTimeDates);

            return mockDateCalculator;
        }

        private static Mock<IRequestRepository> CreateMockRequestRepository()
        {
            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);

            mockRequestRepository
                .Setup(r => r.GetRequests(EarliestConsideredDate, LastConsideredDate))
                .ReturnsAsync(InitialRequests);
            mockRequestRepository
                .Setup(r => r.SaveRequests(
                    It.Is<IReadOnlyCollection<Request>>(actual =>
                        actual.Count == NewlyAllocatedRequests.Count && NewlyAllocatedRequests.All(actual.Contains))))
                .Returns(Task.CompletedTask);

            return mockRequestRepository;
        }
    }
}