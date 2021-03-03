namespace Parking.Data
{
    using System;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using Aws;
    using Business.EmailTemplates;

    public interface IEmailSender
    {
        Task Send(IEmailTemplate emailTemplate);
    }

    public class EmailSender : IEmailSender
    {
        private readonly ISecretProvider secretProvider;

        public EmailSender(ISecretProvider secretProvider) => this.secretProvider = secretProvider;

        public async Task Send(IEmailTemplate emailTemplate)
        {
            const int Port = 587;
            
            const int MaximumSendRate = 10;

            var fromEmailAddress = Environment.GetEnvironmentVariable("FROM_EMAIL_ADDRESS");

            if (string.IsNullOrEmpty(fromEmailAddress))
            {
                return;
            }

            var configSet = Environment.GetEnvironmentVariable("SMTP_CONFIG_SET");
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");

            var password = await this.secretProvider.GetSmtpPassword();

            var message = new MailMessage(fromEmailAddress, emailTemplate.To, emailTemplate.Subject, emailTemplate.PlainTextBody);

            message.Headers.Add("X-SES-CONFIGURATION-SET", configSet);
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    emailTemplate.HtmlBody,
                    new ContentType("text/html")));

            using var client = new SmtpClient(host, Port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            Thread.Sleep(TimeSpan.FromMilliseconds(1000 / (double)MaximumSendRate));

            await client.SendMailAsync(message);
        }
    }
}