using ByteFlow.Protocol;
using System;
using System.Collections.Generic;

namespace ByteFlowTest
{
    [ByteProtoPacket(EntityByteTypes.Demo)]
    public class Entity
    {
        [ByteProtoMember(1)]
        public int Index { get; set; }

        [ByteProtoMember(2)]
        public long Id { get; set; }

        [ByteProtoMember(3)]
        public string Name { get; set; } = string.Empty;

        [ByteProtoMember(4)]
        public SubEntity? SubEntity { get; set; } =null;

        [ByteProtoMember(5)]
        public List<Guid?> List { get; set; } = new();

        [ByteProtoMember(6)]
        public string[] Arr { get; set; } = Array.Empty<string>();

        [ByteProtoMember(7)]
        public List<SubEntity>? Subs { get; set; }

        [ByteProtoMember(8)]
        public List<byte> SubIds { get; set; } = new();

        [ByteProtoMember(9)]
        public TimeSpan Duration { get; set; }

        [ByteProtoMember(10)]
        public DateTimeOffset DateTime { get; set; }

        [ByteProtoMember(11)]
        public bool Checked { get; set; }

        [ByteProtoMember(12)]
        public Guid Uid { get; set; }
    }
}
