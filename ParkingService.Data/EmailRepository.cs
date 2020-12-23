namespace ParkingService.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Business.Data;
    using Business.EmailTemplates;
    using Model;

    public class EmailRepository : IEmailRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public EmailRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task Send(IEmailTemplate emailTemplate) =>
            await rawItemRepository.SendEmail(
                JsonSerializer.Serialize(
                    new Email(
                        emailTemplate.To,
                        emailTemplate.Subject,
                        emailTemplate.PlainTextBody,
                        emailTemplate.HtmlBody)));
    }
}