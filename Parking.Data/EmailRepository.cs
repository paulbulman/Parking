namespace Parking.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Business.EmailTemplates;
    using Microsoft.Extensions.Logging;

    public class EmailRepository : IEmailRepository
    {
        private readonly ILogger<EmailRepository> logger;
        private readonly IEmailProvider emailProvider;
        private readonly IStorageProvider storageProvider;

        public EmailRepository(
            ILogger<EmailRepository> logger,
            IEmailProvider emailProvider,
            IStorageProvider storageProvider)
        {
            this.logger = logger;
            this.storageProvider = storageProvider;
            this.emailProvider = emailProvider;
        }

        public async Task Send(IEmailTemplate emailTemplate)
        {
            var rawData = JsonSerializer.Serialize(emailTemplate);
            
            this.logger.LogInformation("Sending email:\r\n" + rawData);

            await this.storageProvider.SaveEmail(rawData);

            await this.emailProvider.Send(emailTemplate);
        }
    }
}