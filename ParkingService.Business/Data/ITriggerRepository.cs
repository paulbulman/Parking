namespace ParkingService.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ITriggerRepository
    {
        Task<IReadOnlyCollection<string>> GetKeys();

        Task DeleteKeys(IReadOnlyCollection<string> keys);
    }
}