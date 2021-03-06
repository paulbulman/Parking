namespace Parking.Api.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.DynamoDBv2.Model;

    public static class DatabaseHelpers
    {
        private static string TableName => Environment.GetEnvironmentVariable("TABLE_NAME");

        public static async Task ResetDatabase()
        {
            using var client = DatabaseClientFactory.Create();

            await DeleteTableIfExists(client);

            await CreateTable(client);
        }

        public static async Task CreateUser(string userId, string firstName, string lastName)
        {
            using var client = DatabaseClientFactory.Create();

            var table = Table.LoadTable(client, TableName);

            var document = new Document
            {
                ["PK"] = $"USER#{userId}", 
                ["SK"] = "PROFILE",
                ["firstName"] = firstName,
                ["lastName"] = lastName
            };

            await table.PutItemAsync(document);
        }

        public static async Task CreateRequests(string userId, string monthKey, Dictionary<string, string> requests)
        {
            using var client = DatabaseClientFactory.Create();

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