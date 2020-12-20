namespace ParkingService.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Model;

    public class EmailRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public EmailRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task Send(Email email) => await rawItemRepository.SendEmail(JsonSerializer.Serialize(email));
    }
}