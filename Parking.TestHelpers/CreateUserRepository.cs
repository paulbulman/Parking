namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using Moq;

    public static class CreateUserRepository
    {
        public static IUserRepository WithUserExists(string userId, bool userExists)
        {
            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);

            mockUserRepository
                .Setup(r => r.UserExists(userId))
                .ReturnsAsync(userExists);

            return mockUserRepository.Object;
        }

        public static IUserRepository WithUser(string userId, User? user)
        {
            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);

            mockUserRepository
                .Setup(r => r.GetUser(userId))
                .ReturnsAsync(user);

            return mockUserRepository.Object;
        }

        public static IUserRepository WithUsers(IReadOnlyCollection<User> users)
        {
            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            return mockUserRepository.Object;
        }
    }
}