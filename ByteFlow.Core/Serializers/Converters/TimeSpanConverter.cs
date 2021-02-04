using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteFlow.Serializers.Converters
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return TimeSpan.TryParse(reader.GetString(), out TimeSpan span) ? span : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
