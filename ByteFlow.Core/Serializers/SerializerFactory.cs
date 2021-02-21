using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using ByteFlow.Serializers.Converters;

namespace ByteFlow.Serializers
{
    public static class SerializerFactory
    {
        private static readonly IJsonSerializer TextJsonSerializer = new TextJsonSerializer();

        public static Action<JsonSerializerOptions>? JsonSerializerOptionsConfigurator { get; set; }

        public static IJsonSerializer GetTextJsonSerializer() => TextJsonSerializer;

        public static void ConfigureJsonSerializerOptions(JsonSerializerOptions options)
        {
            options.AllowTrailingCommas = true;
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.IgnoreNullValues = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new TimeSpanConverter());
            options.Converters.Add(new DateTimeOffsetConverter());
            JsonSerializerOptionsConfigurator?.Invoke(options);
        }
    }
}
