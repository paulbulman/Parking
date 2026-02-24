namespace Parking.Api.IntegrationTests;

using TestHelpers.Aws;
using Xunit;

[CollectionDefinition("Database tests")]
public class DatabaseCollection
    : ICollectionFixture<LocalStackFixture>,
      ICollectionFixture<CustomWebApplicationFactory<Startup>>
{
}