using System;

namespace ByteFlow.Protocol
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ByteProtoMemberAttribute : Attribute
    {
        public ByteProtoMemberAttribute(int order)
        {
            this.Order = order;
        }

        /// <summary>
        /// 在序列化时的写入顺序
        /// </summary>
        public int Order { get; }

        public override string ToString() => $"Order:{Order}";
    }
}
