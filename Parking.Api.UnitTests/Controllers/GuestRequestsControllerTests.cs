namespace Parking.Api.UnitTests.Controllers;

using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Json.GuestRequests;
using Business;
using Business.Data;
using Microsoft.AspNetCore.Mvc;
using Model;
using Moq;
using NodaTime;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;

public static class GuestRequestsControllerTests
{
    public static class Post
    {
        [Fact]
        public static async Task Creates_guest_request_successfully()
        {
            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([]);
            mockGuestRequestRepository
                .Setup(r => r.SaveGuestRequest(It.Is<GuestRequest>(g =>
                    g.Date == 15.March(2026) &&
                    g.Name == "Alice Smith" &&
                    g.VisitingUserId == "user1" &&
                    g.RegistrationNumber == "AB12CDE" &&
                    g.Status == GuestRequestStatus.Pending &&
                    !string.IsNullOrEmpty(g.Id))))
                .Returns(Task.CompletedTask);

            var mockTriggerRepository = new Mock<ITriggerRepository>();

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "John", lastName: "Doe"),
            };

            var activeDates = new[] { 15.March(2026), 16.March(2026) };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                triggerRepository: mockTriggerRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users),
                activeDates: activeDates);

            var request = new GuestRequestsPostRequest(
                15.March(2026), "Alice Smith", "user1", "AB12CDE");

            var result = await controller.PostAsync(request);

            Assert.IsType<OkResult>(result);
            mockGuestRequestRepository.VerifyAll();
        }

        [Fact]
        public static async Task Triggers_allocation_rerun()
        {
            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>();
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([]);

            var mockTriggerRepository = new Mock<ITriggerRepository>(MockBehavior.Strict);
            mockTriggerRepository
                .Setup(r => r.AddTrigger())
                .Returns(Task.CompletedTask);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                triggerRepository: mockTriggerRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPostRequest(15.March(2026), "Guest", "user1", null);

            await controller.PostAsync(request);

            mockTriggerRepository.VerifyAll();
        }

        [Fact]
        public static async Task Rejects_empty_name()
        {
            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPostRequest(15.March(2026), "", "user1", null);

            var result = await controller.PostAsync(request);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public static async Task Rejects_nonexistent_visiting_user()
        {
            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPostRequest(15.March(2026), "Alice Smith", "nonexistent-user", null);

            var result = await controller.PostAsync(request);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public static async Task Rejects_duplicate_name_on_same_date()
        {
            var existingGuest = new GuestRequest(
                id: "existing-id",
                date: 15.March(2026),
                name: "alice smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPostRequest(15.March(2026), "Alice Smith", "user1", null);

            var result = await controller.PostAsync(request);

            Assert.IsType<ConflictResult>(result);
        }

        [Fact]
        public static async Task Allows_same_name_on_different_dates()
        {
            var existingGuest = new GuestRequest(
                id: "existing-id",
                date: 16.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);
            mockGuestRequestRepository
                .Setup(r => r.SaveGuestRequest(It.IsAny<GuestRequest>()))
                .Returns(Task.CompletedTask);

            var mockTriggerRepository = new Mock<ITriggerRepository>();
            mockTriggerRepository
                .Setup(r => r.AddTrigger())
                .Returns(Task.CompletedTask);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                triggerRepository: mockTriggerRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPostRequest(15.March(2026), "Alice Smith", "user1", null);

            var result = await controller.PostAsync(request);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public static async Task Rejects_date_not_in_active_dates()
        {
            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                userRepository: CreateUserRepository.WithUsers(users),
                activeDates: [16.March(2026), 17.March(2026)]);

            var request = new GuestRequestsPostRequest(15.March(2026), "Alice Smith", "user1", null);

            var result = await controller.PostAsync(request);

            Assert.IsType<BadRequestResult>(result);
        }

        private static GuestRequestsController CreateController(
            IGuestRequestRepository? guestRequestRepository = null,
            ITriggerRepository? triggerRepository = null,
            IUserRepository? userRepository = null,
            IReadOnlyCollection<LocalDate>? activeDates = null)
        {
            var dateCalculator = CreateDateCalculator.WithActiveDates(
                activeDates ?? [15.March(2026)]);

            return new GuestRequestsController(
                dateCalculator,
                guestRequestRepository ?? Mock.Of<IGuestRequestRepository>(),
                triggerRepository ?? Mock.Of<ITriggerRepository>(),
                userRepository ?? Mock.Of<IUserRepository>());
        }
    }

    public static class Put
    {
        [Fact]
        public static async Task Updates_guest_request_successfully()
        {
            var existingGuest = new GuestRequest(
                id: "guest-id-1",
                date: 15.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Allocated);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);
            mockGuestRequestRepository
                .Setup(r => r.UpdateGuestRequest(It.Is<GuestRequest>(g =>
                    g.Id == "guest-id-1" &&
                    g.Date == 15.March(2026) &&
                    g.Name == "Alice Jones" &&
                    g.VisitingUserId == "user2" &&
                    g.RegistrationNumber == "XY99ZZZ" &&
                    g.Status == GuestRequestStatus.Allocated)))
                .Returns(Task.CompletedTask);

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "John", lastName: "Doe"),
                CreateUser.With(userId: "user2", firstName: "Jane", lastName: "Doe"),
            };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPutRequest("Alice Jones", "user2", "XY99ZZZ");

            var result = await controller.PutAsync("2026-03-15", "guest-id-1", request);

            Assert.IsType<OkResult>(result);
            mockGuestRequestRepository.VerifyAll();
        }

        [Fact]
        public static async Task Returns_not_found_for_nonexistent_guest()
        {
            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([]);

            var controller = CreateController(guestRequestRepository: mockGuestRequestRepository.Object);

            var request = new GuestRequestsPutRequest("Alice Smith", "user1", null);

            var result = await controller.PutAsync("2026-03-15", "nonexistent-id", request);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public static async Task Rejects_nonexistent_visiting_user()
        {
            var existingGuest = new GuestRequest(
                id: "guest-id-1",
                date: 15.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPutRequest("Alice Smith", "nonexistent-user", null);

            var result = await controller.PutAsync("2026-03-15", "guest-id-1", request);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public static async Task Rejects_duplicate_name_on_same_date()
        {
            var existingGuest = new GuestRequest(
                id: "guest-id-1",
                date: 15.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var otherGuest = new GuestRequest(
                id: "guest-id-2",
                date: 15.March(2026),
                name: "bob jones",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest, otherGuest]);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPutRequest("Bob Jones", "user1", null);

            var result = await controller.PutAsync("2026-03-15", "guest-id-1", request);

            Assert.IsType<ConflictResult>(result);
        }

        [Fact]
        public static async Task Allows_same_name_when_unchanged()
        {
            var existingGuest = new GuestRequest(
                id: "guest-id-1",
                date: 15.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);
            mockGuestRequestRepository
                .Setup(r => r.UpdateGuestRequest(It.IsAny<GuestRequest>()))
                .Returns(Task.CompletedTask);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPutRequest("Alice Smith", "user1", null);

            var result = await controller.PutAsync("2026-03-15", "guest-id-1", request);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public static async Task Does_not_trigger_allocation_rerun()
        {
            var existingGuest = new GuestRequest(
                id: "guest-id-1",
                date: 15.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>();
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);
            mockGuestRequestRepository
                .Setup(r => r.UpdateGuestRequest(It.IsAny<GuestRequest>()))
                .Returns(Task.CompletedTask);

            var mockTriggerRepository = new Mock<ITriggerRepository>(MockBehavior.Strict);

            var users = new[] { CreateUser.With(userId: "user1") };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                triggerRepository: mockTriggerRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users));

            var request = new GuestRequestsPutRequest("Alice Smith", "user1", null);

            await controller.PutAsync("2026-03-15", "guest-id-1", request);

            mockTriggerRepository.VerifyAll();
        }

        private static GuestRequestsController CreateController(
            IGuestRequestRepository? guestRequestRepository = null,
            ITriggerRepository? triggerRepository = null,
            IUserRepository? userRepository = null)
        {
            var dateCalculator = CreateDateCalculator.WithActiveDates([15.March(2026)]);

            return new GuestRequestsController(
                dateCalculator,
                guestRequestRepository ?? Mock.Of<IGuestRequestRepository>(),
                triggerRepository ?? Mock.Of<ITriggerRepository>(),
                userRepository ?? Mock.Of<IUserRepository>());
        }
    }

    public static class Delete
    {
        [Fact]
        public static async Task Deletes_guest_request_successfully()
        {
            var existingGuest = new GuestRequest(
                id: "guest-id-1",
                date: 15.March(2026),
                name: "Alice Smith",
                visitingUserId: "user1",
                registrationNumber: null,
                status: GuestRequestStatus.Pending);

            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([existingGuest]);
            mockGuestRequestRepository
                .Setup(r => r.DeleteGuestRequest(15.March(2026), "guest-id-1"))
                .Returns(Task.CompletedTask);

            var mockTriggerRepository = new Mock<ITriggerRepository>(MockBehavior.Strict);
            mockTriggerRepository
                .Setup(r => r.AddTrigger())
                .Returns(Task.CompletedTask);

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                triggerRepository: mockTriggerRepository.Object);

            var result = await controller.DeleteAsync("2026-03-15", "guest-id-1");

            Assert.IsType<OkResult>(result);
            mockGuestRequestRepository.VerifyAll();
            mockTriggerRepository.VerifyAll();
        }

        [Fact]
        public static async Task Returns_not_found_for_nonexistent_guest()
        {
            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([]);

            var controller = CreateController(guestRequestRepository: mockGuestRequestRepository.Object);

            var result = await controller.DeleteAsync("2026-03-15", "nonexistent-id");

            Assert.IsType<NotFoundResult>(result);
        }

        private static GuestRequestsController CreateController(
            IGuestRequestRepository? guestRequestRepository = null,
            ITriggerRepository? triggerRepository = null,
            IUserRepository? userRepository = null,
            IReadOnlyCollection<LocalDate>? activeDates = null)
        {
            var dateCalculator = CreateDateCalculator.WithActiveDates(
                activeDates ?? [15.March(2026)]);

            return new GuestRequestsController(
                dateCalculator,
                guestRequestRepository ?? Mock.Of<IGuestRequestRepository>(),
                triggerRepository ?? Mock.Of<ITriggerRepository>(),
                userRepository ?? Mock.Of<IUserRepository>());
        }
    }
}
