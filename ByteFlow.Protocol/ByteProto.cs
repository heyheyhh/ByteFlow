using ByteFlow.Streams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ByteFlow.Protocol
{
    /// <summary>
    /// 第一个字节为版本号，最大可为200，200~255为保留段
    /// 200为心跳请求，201为心跳响应
    /// </summary>
    public static partial class ByteProto
    {
        public const byte HeartbeatRequestCmd = 200;
        public const byte HeartbeatResponseCmd = 201;
        
        public static readonly byte[] HeartbeatRequestPacket = { HeartbeatRequestCmd };
        public static readonly byte[] HeartbeatResponsePacket = { HeartbeatResponseCmd };
        
        public static byte[] Packet<TObj>(TObj obj, Endian endian = Endian.BIG_ENDIAN, Encoding? textEncoding = null)
            where TObj : class
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"在调用此方法前，应先调用 {nameof(RegisterTypes)} 来注册 ByteProto 即将用到的实体类型");
            }

            var desc = GetTargetDescriptor(typeof(TObj));
            if (desc.EntityAttribute is not ByteProtoPacketAttribute packetAttr)
            {
                throw new InvalidOperationException("该实体不能作为单独的网络包发送");
            }

            // Write Body First
            var bodyBytes = ByteProtoSerializer.Serialize(obj, desc, endian, textEncoding);

            using var writer = BytesFactory.GetWriter(endian);
            writer.WriteByte(packetAttr.Version); // 版本号，不同的版本可有不同的序列化方式
            ByteProtoSerializer.WriteCompressedSize(writer, packetAttr.PacketType); // type
            ByteProtoSerializer.WriteCompressedSize(writer, bodyBytes.Length); // body size
            writer.WriteBytes(bodyBytes); // body content

            return writer.Bytes.ToArray();
        }

        public static bool IsHeartbeatRequestPacket(in byte[] packetData)
            => packetData.Length == 1 && packetData[0] == HeartbeatRequestCmd;

        public static bool IsHeartbeatResponsePacket(in byte[] packetData)
            => packetData.Length == 1 && packetData[0] == HeartbeatResponseCmd;

        public static object? Unpacket(in byte[] packetData, Endian endian = Endian.BIG_ENDIAN, Encoding? textEncoding = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"在调用此方法前，应先调用 {nameof(RegisterTypes)} 来注册 ByteProto 即将用到的实体类型");
            }

            var reader = BytesFactory.GetReader(packetData, endian);
            _ = reader.ReadByte(); // 版本号，不同的版本可有不同的序列化方式
            var packetType = ByteProtoSerializer.ReadCompressedSize(reader);
            var size = ByteProtoSerializer.ReadCompressedSize(reader);
            var isPacketValid = reader.Position + size == reader.Data.Length;
            if (!isPacketValid)
            {
                throw new InvalidPacketException("Packet is invalid");
            }

            var desc = GetDescriptorByPacketType(packetType);
            if (desc == null)
            {
                throw new InvalidPacketException($"无法为包:{packetType} 找到合适的解包目标类型");
            }

            if (desc.EntityAttribute is not ByteProtoPacketAttribute)
            {
                throw new InvalidPacketException("无效的网络包");
            }

            var span = new Span<byte>(packetData, reader.Position, packetData.Length - reader.Position);
            return ByteProtoSerializer.Deserialize(desc.Type, span.ToArray(), endian, textEncoding);
        }
    }

    public static partial class ByteProto
    {
        internal static readonly Dictionary<Type, ByteProtoTargetDescriptor> Map = new();
        internal static readonly Dictionary<int, ByteProtoTargetDescriptor> PacketTypeMap = new();
        internal static readonly Type ByteType = typeof(byte);
        internal static readonly Type ShortType = typeof(short);
        internal static readonly Type UShortType = typeof(ushort);
        internal static readonly Type IntType = typeof(int);
        internal static readonly Type UIntType = typeof(uint);
        internal static readonly Type LongType = typeof(long);
        internal static readonly Type ULongType = typeof(ulong);
        internal static readonly Type FloatType = typeof(float);
        internal static readonly Type DoubleType = typeof(double);
        internal static readonly Type StringType = typeof(string);
        internal static readonly Type TimeSpanType = typeof(TimeSpan);
        internal static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
        internal static readonly Type ListType = typeof(IList);
        internal static readonly Type BoolType = typeof(bool);
        internal static readonly Type GuidType = typeof(Guid);

        internal static bool IsInitialized { get; private set; }

        public static void RegisterTypes(params Assembly[] entityAssemblies)
        {
            Map.Clear();
            PacketTypeMap.Clear();

            var entryAsm = Assembly.GetEntryAssembly();
            IEnumerable<Assembly> assemblyCollection =
                entityAssemblies.Length <= 0 ?
                (entryAsm != null ? new[] { entryAsm } : Array.Empty<Assembly>()) :
                entityAssemblies;

            foreach (var assembly in assemblyCollection)
            {
                var types = assembly.ExportedTypes;
                foreach (var type in types)
                {
                    if (type.IsAbstract || !type.IsClass)
                    {
                        continue;
                    }

                    var attr = type.GetCustomAttribute<ByteProtoEntityAttribute>(true);
                    if (attr == null)
                    {
                        continue;
                    }
                    var desc = new ByteProtoTargetDescriptor(type, attr);
                    Map.Add(type, desc);

                    if (attr is ByteProtoPacketAttribute packetAttr)
                    {
                        PacketTypeMap.Add(packetAttr.PacketType, desc);
                    }
                }
            }

            IsInitialized = true;
        }

        internal static ByteProtoTargetDescriptor GetTargetDescriptor(Type type)
        {
            if (Map.TryGetValue(type, out var val))
            {
                return val;
            }

            throw new Exception($"Cannot find {nameof(ByteProtoTargetDescriptor)} for {type}");
        }

        internal static bool TargetHasDescriptor(Type targetType, out ByteProtoTargetDescriptor? desc) => Map.TryGetValue(targetType, out desc);

        internal static ByteProtoTargetDescriptor? GetDescriptorByPacketType(int packetType) => PacketTypeMap.TryGetValue(packetType, out var type) ? type : null;
    }
}
