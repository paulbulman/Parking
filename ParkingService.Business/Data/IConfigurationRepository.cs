namespace ParkingService.Business.Data
{
    using System.Threading.Tasks;
    using Model;

    public interface IConfigurationRepository
    {
        Task<Configuration> GetConfiguration();
    }
}