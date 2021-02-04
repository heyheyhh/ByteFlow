using ByteFlow.Protocol;

namespace ByteFlowTest
{
    [ByteProtoEntity]
    public class SubEntity
    {
        [ByteProtoMember(1)]
        public byte Type { get; set; }

        [ByteProtoMember(2)]
        public double Balance { get; set; }

        [ByteProtoMember(3)]
        public string Currency { get; set; } = "CNY";

        [ByteProtoMember(4)]
        public SubEntity2 Sub { get; set; } = new();
    }
}
