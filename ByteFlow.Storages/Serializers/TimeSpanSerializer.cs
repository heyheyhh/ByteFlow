using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ByteFlow.Storages
{
    public class TimeSpanSerializer : SerializerBase<TimeSpan>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeSpan value)
        {
            context.Writer.WriteString(value.ToString());
        }

        public override TimeSpan Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.CurrentBsonType;
            return type switch
            {
                BsonType.String => TimeSpan.TryParse(context.Reader.ReadString(), out var time) ? time : TimeSpan.Zero,
                _ => throw new NotSupportedException($"Type: {type} 不支持用于 TimeSpan")
            };
        }
    }
}