namespace Parking.Data.Aws
{
    using System;
    using System.Threading.Tasks;
    using Amazon.SimpleSystemsManagement;
    using Amazon.SimpleSystemsManagement.Model;

    public interface ISecretProvider
    {
        Task<string> GetSmtpPassword();
    }

    public class SecretProvider : ISecretProvider
    {
        private readonly IAmazonSimpleSystemsManagement simpleSystemsManagement;

        public SecretProvider(IAmazonSimpleSystemsManagement simpleSystemsManagement) =>
            this.simpleSystemsManagement = simpleSystemsManagement;

        private static string SmtpPasswordKey => Environment.GetEnvironmentVariable("SMTP_PASSWORD_KEY");

        public async Task<string> GetSmtpPassword()
        {
            var request = new GetParameterRequest { Name = SmtpPasswordKey, WithDecryption = true };

            var response = await this.simpleSystemsManagement.GetParameterAsync(request);

            return response.Parameter.Value;
        }
    }
}