namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Business;
    using Business.Data;
    using Model;

    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseProvider databaseProvider;
        
        private readonly IIdentityProvider identityProvider;

        public UserRepository(IDatabaseProvider databaseProvider,  IIdentityProvider identityProvider)
        {
            this.databaseProvider = databaseProvider;
            this.identityProvider = identityProvider;
        }

        public async Task<User> CreateUser(User user)
        {
            var userId = await this.identityProvider.CreateUser(user.EmailAddress, user.FirstName, user.LastName);

            var newUser = new User(
                userId,
                user.AlternativeRegistrationNumber,
                user.CommuteDistance,
                user.EmailAddress,
                user.FirstName,
                user.LastName,
                user.RegistrationNumber);
                
            await this.databaseProvider.SaveItem(CreateRawItem(newUser));

            return newUser;
        }

        public async Task<bool> UserExists(string userId)
        {
            var queryResult = await this.databaseProvider.GetUser(userId);
            
            return queryResult != null;
        }

        public async Task<User> GetUser(string userId)
        {
            var queryResult = await this.databaseProvider.GetUser(userId);

            return queryResult != null ? CreateUser(queryResult) : null;
        }

        public async Task<IReadOnlyCollection<User>> GetUsers()
        {
            var queryResult = await this.databaseProvider.GetUsers();

            return queryResult
                .Select(CreateUser)
                .ToArray();
        }

        public async Task SaveUser(User user)
        {
            await this.identityProvider.UpdateUser(user.UserId, user.FirstName, user.LastName);

            await this.databaseProvider.SaveItem(CreateRawItem(user));
        }

        public async Task<IReadOnlyCollection<User>> GetTeamLeaderUsers()
        {
            var allUsers = await this.GetUsers();

            var teamLeaderUserIds = await this.identityProvider.GetUserIdsInGroup(Constants.TeamLeaderGroupName);

            return allUsers
                .Where(u => teamLeaderUserIds.Contains(u.UserId))
                .ToArray();
        }

        private static User CreateUser(RawItem rawItem) =>
            new User(
                rawItem.PrimaryKey.Split('#')[1],
                rawItem.AlternativeRegistrationNumber,
                rawItem.CommuteDistance,
                rawItem.EmailAddress,
                rawItem.FirstName,
                rawItem.LastName,
                rawItem.RegistrationNumber);

        private static RawItem CreateRawItem(User user) =>
            new RawItem
            {
                PrimaryKey = $"USER#{user.UserId}",
                SortKey = "PROFILE",
                AlternativeRegistrationNumber = user.AlternativeRegistrationNumber,
                CommuteDistance = user.CommuteDistance,
                EmailAddress = user.EmailAddress,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RegistrationNumber = user.RegistrationNumber
            };
    }
}