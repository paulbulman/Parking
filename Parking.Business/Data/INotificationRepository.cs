namespace Parking.Business.Data
{
    using System.Threading.Tasks;

    public interface INotificationRepository
    {
        Task Send(string subject, string body);
    }
}