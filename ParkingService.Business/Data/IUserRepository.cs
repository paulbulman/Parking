namespace ParkingService.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;

    public interface IUserRepository
    {
        Task<IReadOnlyCollection<User>> GetUsers();
    }
}