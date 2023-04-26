namespace Parking.Data
{
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Business.EmailTemplates;
    using Microsoft.Extensions.Logging;

    public class EmailRepository : IEmailRepository
    {
        private readonly ILogger<EmailRepository> logger;
        private readonly IEmailProvider emailProvider;

        public EmailRepository(ILogger<EmailRepository> logger, IEmailProvider emailProvider)
        {
            this.logger = logger;
            this.emailProvider = emailProvider;
        }

        public async Task Send(IEmailTemplate emailTemplate)
        {
            this.logger.LogInformation("Sending email: {@EmailTemplate}", emailTemplate);

            await this.emailProvider.Send(emailTemplate);
        }
    }
}