namespace Parking.Data.Aws
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.SimpleEmail;
    using Amazon.SimpleEmail.Model;
    using Business.EmailTemplates;

    public interface IEmailProvider
    {
        Task Send(IEmailTemplate emailTemplate);
    }

    public class EmailProvider : IEmailProvider
    {
        public static readonly AmazonSimpleEmailServiceConfig Config = new AmazonSimpleEmailServiceConfig
        {
            RegionEndpoint = RegionEndpoint.EUWest1
        };

        private readonly IAmazonSimpleEmailService amazonSimpleEmailService;

        public EmailProvider(IAmazonSimpleEmailService amazonSimpleEmailService) =>
            this.amazonSimpleEmailService = amazonSimpleEmailService;

        public async Task Send(IEmailTemplate emailTemplate)
        {
            const int MaximumSendRate = 10;

            var fromEmailAddress = Environment.GetEnvironmentVariable("FROM_EMAIL_ADDRESS");

            if (string.IsNullOrEmpty(fromEmailAddress))
            {
                return;
            }

            var configSet = Environment.GetEnvironmentVariable("SMTP_CONFIG_SET");

            Thread.Sleep(TimeSpan.FromMilliseconds(1000 / (double)MaximumSendRate));

            await this.amazonSimpleEmailService.SendEmailAsync(new SendEmailRequest
            {
                ConfigurationSetName = configSet,
                Destination = new Destination(new List<string> { emailTemplate.To }),
                Message = new Message
                {
                    Body = new Body
                    {
                        Html = new Content(emailTemplate.HtmlBody),
                        Text = new Content(emailTemplate.PlainTextBody)
                    },
                    Subject = new Content(emailTemplate.Subject)
                },
                Source = fromEmailAddress,
            });
        }
    }
}