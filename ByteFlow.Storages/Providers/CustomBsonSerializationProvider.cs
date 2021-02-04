using System;
using MongoDB.Bson.Serialization;

namespace ByteFlow.Storages
{
#pragma warning disable CS8603 // 可能的 null 引用返回。
    public class CustomBsonSerializationProvider : IBsonSerializationProvider
    {
        private readonly DateTimeOffsetSerializer _dateTimeOffsetSerializer = new();
        private readonly TimeSpanSerializer _timeSpanSerializer = new();
        
        public IBsonSerializer GetSerializer(Type type)
        {
            if (type == typeof(DateTimeOffset))
            {
                return _dateTimeOffsetSerializer;
            }

            return type == typeof(TimeSpan) ? _timeSpanSerializer : null;
        }
    }
}