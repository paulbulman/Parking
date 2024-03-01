namespace Parking.Api.Converters;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Text;

public class LocalDateConverter : JsonConverter<LocalDate>
{
    public override LocalDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions? options)
    {
        var rawValue = reader.GetString();

        if (string.IsNullOrEmpty(rawValue))
        {
            throw new ArgumentException("Raw value was missing");
        }

        return LocalDatePattern.Iso.Parse(rawValue).GetValueOrThrow();
    }

    public override void Write(Utf8JsonWriter writer, LocalDate value, JsonSerializerOptions? options) =>
        writer.WriteStringValue(LocalDatePattern.Iso.Format(value));
}