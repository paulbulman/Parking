namespace ParkingService.Business.Data
{
    using System.Threading.Tasks;
    using EmailTemplates;

    public interface IEmailRepository
    {
        Task Send(IEmailTemplate emailTemplate);
    }
}