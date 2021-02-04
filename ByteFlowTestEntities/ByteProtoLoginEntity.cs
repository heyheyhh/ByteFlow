using ByteFlow.Protocol;

namespace ByteFlowTestEntities
{
    [ByteProtoPacket(100)]
    public class ByteProtoLoginRequest
    {
        [ByteProtoMember(1)]
        public string Account { get; set; }

        [ByteProtoMember(2)]
        public string Token { get; set; }
    }

    [ByteProtoPacket(101)]
    public class ByteProtoLoginResponse
    {
        [ByteProtoMember(1)]
        public byte Code { get; set; }

        [ByteProtoMember(2)]
        public string Description { get; set; }
    }
}
