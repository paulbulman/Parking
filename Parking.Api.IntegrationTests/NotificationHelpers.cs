namespace Parking.Api.IntegrationTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;

    public static class NotificationHelpers
    {
        private static string TopicArn => Environment.GetEnvironmentVariable("TOPIC_NAME");

        public static IAmazonSimpleNotificationService CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonSimpleNotificationServiceConfig { ServiceURL = "http://localhost:4566" };

            return new AmazonSimpleNotificationServiceClient(credentials, config);
        }

        public static async Task ResetNotifications()
        {
            using var client = CreateClient();

            var topicName = TopicArn.Split(":").Last();

            await DeleteTopicIfExists(client, topicName);

            await CreateTopic(client, topicName);

            // SNS uses full ARNs, rather than names, so the region/account ID portions need swapping out for testing
            await OverrideConfigWithFakeArn(client, topicName);
        }

        private static async Task DeleteTopicIfExists(IAmazonSimpleNotificationService client, string topicName)
        {
            var matchingTopic = await GetExistingTopic(client, topicName);

            if (matchingTopic != null)
            {
                await client.DeleteTopicAsync(matchingTopic.TopicArn);
            }
        }

        private static async Task CreateTopic(IAmazonSimpleNotificationService client, string topicName) =>
            await client.CreateTopicAsync(topicName);

        private static async Task<Topic> GetExistingTopic(IAmazonSimpleNotificationService client, string topicName)
        {
            var topics = await client.ListTopicsAsync();

            return topics.Topics.SingleOrDefault(t => t.TopicArn.Contains(topicName));
        }

        private static async Task OverrideConfigWithFakeArn(IAmazonSimpleNotificationService client, string topicName)
        {
            var newTopic = await GetExistingTopic(client, topicName);

            Environment.SetEnvironmentVariable("TOPIC_NAME", newTopic.TopicArn);
        }
    }
}