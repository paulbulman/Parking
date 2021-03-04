namespace Parking.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Business.EmailTemplates;

    public class EmailRepository : IEmailRepository
    {
        private readonly IEmailProvider emailProvider;
        private readonly IStorageProvider storageProvider;

        public EmailRepository(IEmailProvider emailProvider, IStorageProvider storageProvider)
        {
            this.storageProvider = storageProvider;
            this.emailProvider = emailProvider;
        }

        public async Task Send(IEmailTemplate emailTemplate)
        {
            await this.storageProvider.SaveEmail(JsonSerializer.Serialize(emailTemplate));

            await this.emailProvider.Send(emailTemplate);
        }
    }
}