using System;
using System.Collections.Generic;
using System.Reflection;

namespace ByteFlow.Protocol
{
    internal class ByteProtoTargetDescriptor
    {
        public Type Type { get; }

        public ByteProtoEntityAttribute EntityAttribute { get; }

        public List<ByteProtoTargetPropertyDescriptor> PropertyDescriptors { get; }

        public ByteProtoTargetDescriptor(Type targetType, ByteProtoEntityAttribute attr)
        {
            this.Type = targetType;
            this.EntityAttribute = attr;
            this.PropertyDescriptors = new List<ByteProtoTargetPropertyDescriptor>();

            var properties = targetType.GetProperties();
            foreach (var p in properties)
            {
                var memAttr = p.GetCustomAttribute<ByteProtoMemberAttribute>();
                if (memAttr is null)
                {
                    continue;
                }

                var desc = new ByteProtoTargetPropertyDescriptor(p, memAttr);
                this.PropertyDescriptors.Add(desc);
            }

            this.PropertyDescriptors.Sort((a, b) => a.MemberAttribute.Order - b.MemberAttribute.Order);
        }
    }
}
