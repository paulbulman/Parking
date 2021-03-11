namespace Parking.Api.IntegrationTests.Helpers
{
    using System.Text.Json;
    using Converters;

    public static class JsonHelpers
    {
        public static string Serialize<T>(T value)
        {
            var options = new JsonSerializerOptions();

            options.Converters.Add(new LocalDateConverter());

            return JsonSerializer.Serialize(value, options);
        }

        public static T Deserialize<T>(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            options.Converters.Add(new LocalDateConverter());

            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}