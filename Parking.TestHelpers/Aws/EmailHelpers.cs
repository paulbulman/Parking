namespace Parking.TestHelpers.Aws
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.SimpleEmail;
    using Amazon.SimpleEmail.Model;
    using Data;

    public static class EmailHelpers
    {
        private const string ServiceUrl = "http://localhost:4566";

        private const string SesEndpoint = "_aws/ses";

        private static string FromEmailAddress => Helpers.GetRequiredEnvironmentVariable("FROM_EMAIL_ADDRESS");

        private static readonly HttpClient SesHttpClient = new HttpClient
        {
            BaseAddress = new Uri(ServiceUrl),
        };

        public static IAmazonSimpleEmailService CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonSimpleEmailServiceConfig { ServiceURL = ServiceUrl };

            return new AmazonSimpleEmailServiceClient(credentials, config);
        }

        public static async Task<IReadOnlyCollection<SentEmail>> GetSentEmails()
        {
            var httpResponseMessage = await SesHttpClient.GetAsync(SesEndpoint);

            var httpResponseContent = await httpResponseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<SentEmails>(httpResponseContent, options);

            if (result == null)
            {
                throw new InvalidOperationException("Could not deserialize JSON string to requested type.");
            }

            return result.Messages;
        }

        public static async Task ResetEmail()
        {
            using var client = CreateClient();

            await client.VerifyEmailIdentityAsync(new VerifyEmailIdentityRequest { EmailAddress = FromEmailAddress });

            await SesHttpClient.DeleteAsync(SesEndpoint);
        }

        public class SentEmails
        {
            public SentEmails(IReadOnlyCollection<SentEmail> messages) => this.Messages = messages;

            public IReadOnlyCollection<SentEmail> Messages { get; set; }
        }

        public class SentEmail
        {
            public SentEmail(
                string id,
                string region,
                Destination destination,
                string source,
                string subject,
                Body body,
                DateTime timestamp)
            {
                this.Id = id;
                this.Region = region;
                this.Destination = destination;
                this.Source = source;
                this.Subject = subject;
                this.Body = body;
                this.Timestamp = timestamp;
            }

            public string Id { get; }

            public string Region { get; }

            public Destination Destination { get; }

            public string Source { get; }

            public string Subject { get; }

            public Body Body { get; }

            public DateTime Timestamp { get; }
        }

        public class Destination
        {
            public Destination(IReadOnlyCollection<string> toAddresses) => this.ToAddresses = toAddresses;

            public IReadOnlyCollection<string> ToAddresses { get; }
        }

        public class Body
        {
            public Body(string text_part, string html_part)
            {
                this.text_part = text_part;
                this.html_part = html_part;
            }

            public string text_part { get; }
            
            public string html_part { get; }
        }
    }
}