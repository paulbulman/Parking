namespace Parking.Api.IntegrationTests.Helpers;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Converters;

public static class JsonHelpers
{
    public static string Serialize<T>(T value)
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new LocalDateConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return JsonSerializer.Serialize(value, options);
    }

    public static T Deserialize<T>(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        options.Converters.Add(new LocalDateConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        var result = JsonSerializer.Deserialize<T>(json, options);

        if (result == null)
        {
            throw new ArgumentException("Could not deserialize JSON string to requested type.", nameof(json));
        }

        return result;
    }
}