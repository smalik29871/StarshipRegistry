using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarshipRegistry.Helpers
{
    public class SwapiDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateStr = reader.GetString();
            if (string.IsNullOrEmpty(dateStr)) return null;

            if (DateTime.TryParse(dateStr,
                                  CultureInfo.InvariantCulture,
                                  DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                  out DateTime parsedDate))
            {
                return parsedDate;
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
            else
                writer.WriteNullValue();
        }
    }
}