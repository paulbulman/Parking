namespace Parking.Api.IntegrationTests;

using Xunit;
    
[CollectionDefinition("Database tests")]
public class DatabaseCollection : ICollectionFixture<CustomWebApplicationFactory<Startup>>
{
}