namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Model;

    public class UserRepository : IUserRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public UserRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task<User> GetUser(string userId)
        {
            var queryResult = await this.rawItemRepository.GetUser(userId);

            return queryResult != null ? CreateUser(queryResult) : null;
        }

        public async Task<IReadOnlyCollection<User>> GetUsers()
        {
            var queryResult = await this.rawItemRepository.GetUsers();

            return queryResult
                .Select(CreateUser)
                .ToArray();
        }

        public async Task SaveUser(User user) => await this.rawItemRepository.SaveItem(CreateRawItem(user));

        public async Task<IReadOnlyCollection<User>> GetTeamLeaderUsers()
        {
            var allUsers = await this.GetUsers();

            var teamLeaderUserIds = await this.rawItemRepository.GetUserIdsInGroup(Constants.TeamLeaderGroupName);

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