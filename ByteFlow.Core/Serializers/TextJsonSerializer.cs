using System;
using System.Text.Json;

namespace ByteFlow.Serializers
{
    public class TextJsonSerializer : IJsonSerializer
    {
        public JsonSerializerOptions Options { get; }

        public TextJsonSerializer()
        {
            this.Options = new JsonSerializerOptions();
            SerializerFactory.ConfigureJsonSerializerOptions(Options);
        }

        public TValue Deserialize<TValue>(string json)
            => JsonSerializer.Deserialize<TValue>(json, Options);

        public TValue Deserialize<TValue>(ReadOnlySpan<byte> utf8Json)
            => JsonSerializer.Deserialize<TValue>(utf8Json, Options);

        public object? Deserialize(string json, Type returnType)
            => JsonSerializer.Deserialize(json, returnType, Options);

        public object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType)
            => JsonSerializer.Deserialize(utf8Json, returnType, Options);

        public string Serialize<TValue>(TValue value)
            => JsonSerializer.Serialize(value, Options);

        public string Serialize(object value, Type inputType)
            => JsonSerializer.Serialize(value, inputType, Options);

        public byte[] SerializeToBytes<TValue>(TValue value)
            => JsonSerializer.SerializeToUtf8Bytes(value, Options);

        public byte[] SerializeToBytes(object value, Type inputType)
            => JsonSerializer.SerializeToUtf8Bytes(value, inputType, Options);
    }
}
