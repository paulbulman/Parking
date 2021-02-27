namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;

    public interface IUserRepository
    {
        Task<bool> UserExists(string userId);

        Task<User> GetUser(string userId);

        Task<IReadOnlyCollection<User>> GetUsers();

        Task SaveUser(User user);

        Task<IReadOnlyCollection<User>> GetTeamLeaderUsers();
    }
}