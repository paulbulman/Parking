namespace Parking.TestHelpers.Aws
{
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.SimpleEmail;
    using Amazon.SimpleEmail.Model;
    using Data;

    public static class EmailHelpers
    {
        private static string FromEmailAddress => Helpers.GetRequiredEnvironmentVariable("FROM_EMAIL_ADDRESS");

        public static IAmazonSimpleEmailService CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonSimpleEmailServiceConfig { ServiceURL = "http://localhost:4566" };

            return new AmazonSimpleEmailServiceClient(credentials, config);
        }

        public static async Task ResetEmail()
        {
            using var client = CreateClient();

            await client.VerifyEmailIdentityAsync(new VerifyEmailIdentityRequest { EmailAddress = FromEmailAddress });
        }
    }
}