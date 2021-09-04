namespace Parking.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Aws;
    using Business;
    using Business.Data;
    using Business.EmailTemplates;

    public class EmailRepository : IEmailRepository
    {
        private readonly IEmailProvider emailProvider;
        private readonly ILogger logger;
        private readonly IStorageProvider storageProvider;

        public EmailRepository(ILogger logger, IEmailProvider emailProvider, IStorageProvider storageProvider)
        {
            this.logger = logger;
            this.storageProvider = storageProvider;
            this.emailProvider = emailProvider;
        }

        public async Task Send(IEmailTemplate emailTemplate)
        {
            var rawData = JsonSerializer.Serialize(emailTemplate);
            
            this.logger.Log("Sending email:\r\n" + rawData);

            await this.storageProvider.SaveEmail(rawData);

            await this.emailProvider.Send(emailTemplate);
        }
    }
}