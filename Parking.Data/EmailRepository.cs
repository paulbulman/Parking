namespace Parking.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Business.Data;
    using Business.EmailTemplates;

    public class EmailRepository : IEmailRepository
    {
        private readonly IRawItemRepository rawItemRepository;
        private readonly IEmailSender emailSender;

        public EmailRepository(IRawItemRepository rawItemRepository, IEmailSender emailSender)
        {
            this.rawItemRepository = rawItemRepository;
            this.emailSender = emailSender;
        }

        public async Task Send(IEmailTemplate emailTemplate)
        {
            await this.rawItemRepository.SaveEmail(JsonSerializer.Serialize(emailTemplate));

            await this.emailSender.Send(emailTemplate);
        }
    }
}