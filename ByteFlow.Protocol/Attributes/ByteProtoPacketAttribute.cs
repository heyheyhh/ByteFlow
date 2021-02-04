using System;

namespace ByteFlow.Protocol
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ByteProtoPacketAttribute : ByteProtoEntityAttribute
    {
        /// <summary>
        /// 描述一个网络包
        /// </summary>
        public ByteProtoPacketAttribute(int packetType)
        {
            this.PacketType = packetType;
        }

        public int PacketType { get; }

        public byte Version { get; set; } = 0x01;
    }
}
