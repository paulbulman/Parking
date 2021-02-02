namespace Parking.Api.UnitTests.Converters
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using Api.Converters;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class LocalDateConverterTests
    {
        [Fact]
        public static void Reads_value_from_ISO_string()
        {
            var rawValue = Encoding.UTF8.GetBytes("\"2021-02-03\"");
            var reader = new Utf8JsonReader(rawValue.AsSpan());
            
            Assert.Equal(JsonTokenType.None, reader.TokenType);
            reader.Read();

            var result = new LocalDateConverter().Read(ref reader, typeof(LocalDate), null);
            Assert.Equal(3.February(2021), result);
        }

        [Fact]
        public static void Writes_value_to_ISO_string()
        {
            using var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            
            new LocalDateConverter().Write(writer, 3.February(2021), null);
            writer.Flush();
                
            var result = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("\"2021-02-03\"", result);
        }
    }
}