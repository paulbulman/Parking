namespace ParkingService.Data
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
                .Select(r => new User(GetUserId(r.PrimaryKey), r.CommuteDistance))
                .ToArray();
        }

        private static string GetUserId(string primaryKey) => primaryKey.Split('#')[1];
    }
}