namespace Parking.TestHelpers.Aws
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.DynamoDBv2.Model;
    using Amazon.Runtime;
    using Data;
    using Model;

    public static class DatabaseHelpers
    {
        private static string TableName => Helpers.GetRequiredEnvironmentVariable("TABLE_NAME");

        public static IAmazonDynamoDB CreateClient()
        {
            var credentials = new BasicAWSCredentials("__ACCESS_KEY__", "__SECRET_KEY__");

            var config = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:4566" };

            return new AmazonDynamoDBClient(credentials, config);
        }

        public static async Task ResetDatabase()
        {
            using var client = CreateClient();

            await DeleteTableIfExists(client);

            await CreateTable(client);
        }

        public static async Task CreateUser(User user)
        {
            using var client = CreateClient();

            var table = Table.LoadTable(client, TableName);

            var document = new Document
            {
                ["PK"] = $"USER#{user.UserId}",
                ["SK"] = "PROFILE",
                ["alternativeRegistrationNumber"] = user.AlternativeRegistrationNumber,
                ["commuteDistance"] = user.CommuteDistance,
                ["emailAddress"] = user.EmailAddress,
                ["firstName"] = user.FirstName,
                ["lastName"] = user.LastName,
                ["registrationNumber"] = user.RegistrationNumber
            };

            await table.PutItemAsync(document);
        }

        public static async Task<User> ReadUser(string userId)
        {
            using var client = CreateClient();

            var table = Table.LoadTable(client, TableName);

            var document = await table.GetItemAsync(new Primitive($"USER#{userId}"), new Primitive("PROFILE"));

            return new User(
                userId,
                document["alternativeRegistrationNumber"],
                decimal.Parse(document["commuteDistance"]),
                document["emailAddress"],
                document["firstName"],
                document["lastName"],
                document["registrationNumber"]);
        }

        public static async Task CreateRequests(string userId, string monthKey, Dictionary<string, string> requests)
        {
            using var client = CreateClient();

            var table = Table.LoadTable(client, TableName);

            var document = new Document
            {
                ["PK"] = $"USER#{userId}",
                ["SK"] = $"REQUESTS#{monthKey}",
                ["requests"] = new Document(requests.ToDictionary(
                    day => day.Key,
                    day => (DynamoDBEntry)new Primitive(day.Value)))
            };

            await table.PutItemAsync(document);
        }

        public static async Task<IDictionary<string, string>> ReadRequests(string userId, string monthKey)
        {
            using var client = CreateClient();

            var table = Table.LoadTable(client, TableName);

            var document = await table.GetItemAsync(new Primitive($"USER#{userId}"), new Primitive($"REQUESTS#{monthKey}"));

            return document["requests"].AsDocument().ToDictionary(
                dailyData => dailyData.Key,
                dailyData => dailyData.Value.AsString());
        }

        public static async Task CreateReservations(string monthKey, Dictionary<string, List<string>> reservations)
        {
            using var client = CreateClient();

            var table = Table.LoadTable(client, TableName);

            var document = new Document
            {
                ["PK"] = "GLOBAL",
                ["SK"] = $"RESERVATIONS#{monthKey}",
                ["reservations"] = new Document(reservations.ToDictionary(
                    day => day.Key,
                    day => (DynamoDBEntry)new DynamoDBList(day.Value.Select(r => new Primitive(r)))))
            };

            await table.PutItemAsync(document);
        }

        public static async Task<IDictionary<string, IReadOnlyCollection<string>>> ReadReservations(string monthKey)
        {
            using var client = CreateClient();

            var table = Table.LoadTable(client, TableName);

            var document = await table.GetItemAsync(new Primitive("GLOBAL"), new Primitive($"RESERVATIONS#{monthKey}"));

            return document["reservations"].AsDocument().ToDictionary(
                dailyData => dailyData.Key,
                dailyData => (IReadOnlyCollection<string>)dailyData.Value
                    .AsDynamoDBList()
                    .Entries
                    .Select(e => e.AsString())
                    .ToArray());
        }

        private static async Task DeleteTableIfExists(IAmazonDynamoDB client)
        {
            var tables = await client.ListTablesAsync();

            if (tables.TableNames.Contains(TableName))
            {
                await client.DeleteTableAsync(new DeleteTableRequest(TableName));
            }
        }

        private static async Task CreateTable(IAmazonDynamoDB client)
        {
            await client.CreateTableAsync(new CreateTableRequest
            {
                TableName = TableName,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition {AttributeName = "PK", AttributeType = "S"},
                    new AttributeDefinition {AttributeName = "SK", AttributeType = "S"}
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement {AttributeName = "PK", KeyType = KeyType.HASH},
                    new KeySchemaElement {AttributeName = "SK", KeyType = KeyType.RANGE}
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "SK-PK-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement {AttributeName = "SK", KeyType = KeyType.HASH},
                            new KeySchemaElement {AttributeName = "PK", KeyType = KeyType.RANGE}
                        },
                        Projection = new Projection {ProjectionType = ProjectionType.ALL},
                        ProvisionedThroughput = new ProvisionedThroughput(100, 100)
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput(100, 100)
            });
        }
    }
}