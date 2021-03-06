namespace Parking.Api.IntegrationTests
{
    using System.Text.Json;
    using Converters;

    public static class JsonHelpers
    {
        public static T Deserialize<T>(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            options.Converters.Add(new LocalDateConverter());

            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}