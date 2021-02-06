namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Model;

    public class UserRepository : IUserRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public UserRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task<IReadOnlyCollection<User>> GetUsers()
        {
            var queryResult = await rawItemRepository.GetUsers();

            return queryResult
                .Select(r => new User(GetUserId(r.PrimaryKey), r.CommuteDistance, r.EmailAddress, r.FirstName, r.LastName))
                .ToArray();
        }

        public async Task<IReadOnlyCollection<User>> GetTeamLeaderUsers()
        {
            const string TeamLeaderGroupName = "TeamLeader";

            var allUsers = await this.GetUsers();

            var teamLeaderUserIds = await this.rawItemRepository.GetUserIdsInGroup(TeamLeaderGroupName);

            return allUsers
                .Where(u => teamLeaderUserIds.Contains(u.UserId))
                .ToArray();
        }

        private static string GetUserId(string primaryKey) => primaryKey.Split('#')[1];
    }
}