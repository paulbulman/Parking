namespace Parking.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Business.EmailTemplates;

    public class EmailRepository : IEmailRepository
    {
        private readonly IStorageProvider storageProvider;
        private readonly IEmailSender emailSender;

        public EmailRepository(IStorageProvider storageProvider, IEmailSender emailSender)
        {
            this.storageProvider = storageProvider;
            this.emailSender = emailSender;
        }

        public async Task Send(IEmailTemplate emailTemplate)
        {
            await this.storageProvider.SaveEmail(JsonSerializer.Serialize(emailTemplate));

            await this.emailSender.Send(emailTemplate);
        }
    }
}