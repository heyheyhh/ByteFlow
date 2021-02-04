using ByteFlow.Protocol;

namespace ByteFlowTest
{
    [ByteProtoEntity]
    public class SubEntity2
    {
        [ByteProtoMember(1)]
        public string Name { get; set; } = string.Empty;
    }
}
