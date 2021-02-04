using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ByteFlow.Storages
{
    public class DateTimeOffsetSerializer : SerializerBase<DateTimeOffset>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTimeOffset value)
        {
            context.Writer.WriteString(value.ToString());
        }

        public override DateTimeOffset Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.CurrentBsonType;
            switch (type)
            {
                case BsonType.String:
                    return DateTimeOffset.TryParse(context.Reader.ReadString(), out var time)
                        ? time
                        : DateTimeOffset.MinValue;
                case BsonType.Array:
                {
                    context.Reader.ReadStartArray();
                    var ticks = context.Reader.ReadInt64();
                    var zone = context.Reader.ReadInt32();
                    context.Reader.ReadEndArray();
                    return new DateTimeOffset(ticks, TimeSpan.FromMinutes(zone));
                }
                default:
                    throw new NotSupportedException($"Type: {type} 不支持用于 DateTimeOffset");
            }
        }
    }
}