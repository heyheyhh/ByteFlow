using System;

namespace ByteFlow.Serializers
{
    public interface IJsonSerializer
    {
        string Serialize<TValue>(TValue value);

        string Serialize(object value, Type inputType);

        byte[] SerializeToBytes<TValue>(TValue value);

        byte[] SerializeToBytes(object value, Type inputType);

        TValue? Deserialize<TValue>(string json);

        object? Deserialize(string json, Type returnType);

        TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json);

        object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType);
    }
}
