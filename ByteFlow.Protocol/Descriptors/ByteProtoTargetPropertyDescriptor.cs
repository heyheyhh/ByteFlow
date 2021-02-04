using System.Reflection;

namespace ByteFlow.Protocol
{
    internal class ByteProtoTargetPropertyDescriptor
    {
        public PropertyInfo PropertyInfo { get; }

        public ByteProtoMemberAttribute MemberAttribute { get; }

        public ByteProtoTargetPropertyDescriptor(PropertyInfo p, ByteProtoMemberAttribute attr)
        {
            this.PropertyInfo = p;
            this.MemberAttribute = attr;
        }
    }
}
