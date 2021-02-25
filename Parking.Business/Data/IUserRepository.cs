namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;

    public interface IUserRepository
    {
        Task<User> GetUser(string userId);

        Task<IReadOnlyCollection<User>> GetUsers();

        Task<IReadOnlyCollection<User>> GetTeamLeaderUsers();
    }
}