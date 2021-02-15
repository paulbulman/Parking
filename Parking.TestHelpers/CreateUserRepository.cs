namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using Moq;

    public static class CreateUserRepository
    {
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