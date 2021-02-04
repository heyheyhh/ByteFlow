using ByteFlow.Streams;
using ByteFlow.Streams.Abstractions;
using System;
using System.Collections;
using System.Text;

namespace ByteFlow.Protocol
{
    public static partial class ByteProtoSerializer
    {
        public static TOut? Deserialize<TOut>(byte[] rawData, Endian endian = Endian.BIG_ENDIAN, Encoding? textEncoding = null)
            where TOut : class
            => Deserialize(typeof(TOut), rawData, endian, textEncoding) as TOut;

        public static object Deserialize(Type outType, byte[] rawData, Endian endian = Endian.BIG_ENDIAN, Encoding? textEncoding = null)
        {
            if (!ByteProto.TargetHasDescriptor(outType, out var desc) || desc == null)
            {
                throw new Exception($"cannot find a class for type: {outType} ");
            }

            var reader = BytesFactory.GetReader(rawData, endian);
            return ReadObject(reader, desc, textEncoding ?? Encoding.UTF8);
        }

        public static byte[] Serialize<TObj>(TObj obj, Endian endian = Endian.BIG_ENDIAN, Encoding? textEncoding = null)
            where TObj : class
        {
            using var bodyWriter = BytesFactory.GetWriter(endian);
            // Write Body
            var desc = ByteProto.GetTargetDescriptor(typeof(TObj));
            WriteObject(desc, obj, bodyWriter, textEncoding ?? Encoding.UTF8);

            return bodyWriter.Bytes.ToArray();
        }
    }

    public static partial class ByteProtoSerializer
    {
        private enum ByteProtoSizeCompressType : byte
        {
            /// <summary>
            /// 最大长度为 <see cref="int.MaxValue"/>
            /// </summary>
            FourByte,
            /// <summary>
            /// 最大长度为  <see cref="ushort.MaxValue"/>
            /// </summary>
            TwoByte,
            /// <summary>
            /// 最大长度为 <see cref="byte.MaxValue"/>
            /// </summary>
            OneByte,
            /// <summary>
            /// 当目标长度为0时，使用此标志，此时可不写入长度信息
            /// </summary>
            ZeroByte
        }

        internal static byte[] Serialize(object obj, ByteProtoTargetDescriptor desc, Endian endian = Endian.BIG_ENDIAN, Encoding? textEncoding = null)
        {
            using var bodyWriter = BytesFactory.GetWriter(endian);
            // Write Body
            WriteObject(desc, obj, bodyWriter, textEncoding ?? Encoding.UTF8);

            return bodyWriter.Bytes.ToArray();
        }

        internal static int ReadCompressedSize(IBytesReader reader)
        {
            var sizeType = reader.ReadByte();
            return sizeType switch
            {
                (byte) ByteProtoSizeCompressType.ZeroByte => 0,
                (byte) ByteProtoSizeCompressType.OneByte => reader.ReadByte(),
                (byte) ByteProtoSizeCompressType.TwoByte => reader.ReadUShort(),
                _ => reader.ReadInt()
            };
        }

        internal static void WriteCompressedSize(IBytesWriter writer, int size)
        {
            switch (size)
            {
                case 0:
                    writer.WriteByte((byte)ByteProtoSizeCompressType.ZeroByte); // 此时不需要写入长度信息
                    return;
                case <= byte.MaxValue:
                    writer.WriteByte((byte)ByteProtoSizeCompressType.OneByte);
                    writer.WriteByte((byte)size);
                    break;
                case <= ushort.MaxValue:
                    writer.WriteByte((byte)ByteProtoSizeCompressType.TwoByte);
                    writer.WriteUShort((ushort)size);
                    break;
                default:
                    writer.WriteByte((byte)ByteProtoSizeCompressType.FourByte);
                    writer.WriteInt(size);
                    break;
            }
        }

        private static object ReadObject(IBytesReader reader, ByteProtoTargetDescriptor desc, Encoding textEncoding)
        {
            var obj = Activator.CreateInstance(desc.Type);
            if (obj == null)
            {
                throw new Exception($"cannot create instance for {desc.Type}");
            }

            foreach (var pdesc in desc.PropertyDescriptors)
            {
                var value = ReadTypeValue(reader, pdesc.PropertyInfo.PropertyType, textEncoding);
                pdesc.PropertyInfo.SetValue(obj, value);
            }
            return obj;
        }

        private static object ReadTypeValue(IBytesReader reader, Type targetType, Encoding textEncoding)
        {
            if (targetType == ByteProto.ByteType)
            {
                return reader.ReadByte();
            }
            else if (targetType == ByteProto.ShortType)
            {
                return reader.ReadShort();
            }
            else if (targetType == ByteProto.UShortType)
            {
                return reader.ReadUShort();
            }
            else if (targetType == ByteProto.IntType)
            {
                return reader.ReadInt();
            }
            else if (targetType == ByteProto.UIntType)
            {
                return reader.ReadUInt();
            }
            else if (targetType == ByteProto.LongType)
            {
                return reader.ReadLong();
            }
            else if (targetType == ByteProto.ULongType)
            {
                return reader.ReadULong();
            }
            else if (targetType == ByteProto.FloatType)
            {
                return reader.ReadFloat();
            }
            else if (targetType == ByteProto.DoubleType)
            {
                return reader.ReadDouble();
            }
            else if (targetType == ByteProto.StringType)
            {
                var valSize = ReadCompressedSize(reader);
                return reader.ReadString(valSize, textEncoding);
            }
            else if (targetType == ByteProto.TimeSpanType)
            {
                var timespan = reader.ReadUInt();
                return TimeSpan.FromSeconds(timespan);
            }
            else if (targetType == ByteProto.DateTimeOffsetType)
            {
                var ms = reader.ReadLong();
                return DateTimeOffset.FromUnixTimeMilliseconds(ms);
            }
            else if (targetType == ByteProto.BoolType)
            {
                return reader.ReadBool();
            }
            else if (targetType == ByteProto.GuidType)
            {
                var bytes = reader.ReadBytes(16);
                return new Guid(bytes);
            }
            else if (ByteProto.TargetHasDescriptor(targetType, out var objDesc) && objDesc != null)
            {
                return ReadObject(reader, objDesc, textEncoding);
            }
            else if (targetType.IsAssignableTo(ByteProto.ListType))
            {
                var count = ReadCompressedSize(reader);

                IList? coll = null;
                Type? itemType = null;
                if (targetType.IsGenericType && targetType.GenericTypeArguments.Length > 0)
                {
                    itemType = targetType.GenericTypeArguments[0];
                    if (targetType.IsAbstract || targetType.IsInterface)
                    {
                        coll = Array.CreateInstance(itemType, count);
                    }
                    else
                    {
                        coll = Activator.CreateInstance(targetType, count) as IList;
                    }
                }
                else if (targetType.IsArray && !string.IsNullOrWhiteSpace(targetType.FullName))
                {
                    var fullName = targetType.FullName[0..^2];
                    itemType = Type.GetType(fullName);
                    coll = Array.CreateInstance(itemType!, count);
                }

                if (coll == null)
                {
                    throw new Exception($"Cannot create instance for type {targetType}");
                }

                for (var i = 0; i < count; i++)
                {
                    var val = ReadTypeValue(reader, itemType!, textEncoding);
                    if (coll.IsFixedSize)
                    {
                        coll[i] = val;
                    }
                    else
                    {
                        coll.Add(val);
                    }
                }
                return coll;
            }
            else
            {
                throw new NotSupportedException($"target type {targetType} is not supported yet!");
            }
        }

        private static void WriteObject(ByteProtoTargetDescriptor desc, object obj, IBytesWriter writer, Encoding textEncoding)
        {
            foreach (var pdesc in desc.PropertyDescriptors)
            {
                WriteTypeValue(pdesc.PropertyInfo.PropertyType, pdesc.PropertyInfo.GetValue(obj), writer, textEncoding);
            }
        }

        private static void WriteTypeValue(Type pType, object? value, IBytesWriter writer, Encoding textEncoding)
        {
            if (value == null)
            {
                throw new Exception("Value cannot be null");
            }

            if (pType == ByteProto.ByteType)
            {
                writer.WriteByte((byte)value);
            }
            else if (pType == ByteProto.ShortType)
            {
                writer.WriteShort((short)value);
            }
            else if (pType == ByteProto.UShortType)
            {
                writer.WriteUShort((ushort)value);
            }
            else if (pType == ByteProto.IntType)
            {
                writer.WriteInt((int)value);
            }
            else if (pType == ByteProto.UIntType)
            {
                writer.WriteUInt((uint)value);
            }
            else if (pType == ByteProto.LongType)
            {
                writer.WriteLong((long)value);
            }
            else if (pType == ByteProto.ULongType)
            {
                writer.WriteULong((ulong)value);
            }
            else if (pType == ByteProto.FloatType)
            {
                writer.WriteFloat((float)value);
            }
            else if (pType == ByteProto.DoubleType)
            {
                writer.WriteDouble((double)value);
            }
            else if (pType == ByteProto.StringType)
            {
                var bytes = textEncoding.GetBytes((string)value);
                WriteCompressedSize(writer, bytes.Length);
                writer.WriteBytes(bytes);
            }
            else if (pType == ByteProto.TimeSpanType)
            {
                var timespan = (TimeSpan)value;
                writer.WriteUInt((uint)timespan.TotalSeconds);
            }
            else if (pType == ByteProto.DateTimeOffsetType)
            {
                var time = (DateTimeOffset)value;
                writer.WriteLong(time.ToUnixTimeMilliseconds());
            }
            else if (pType == ByteProto.BoolType)
            {
                writer.WriteBool((bool)value);
            }
            else if (pType == ByteProto.GuidType)
            {
                var val = (Guid)value;
                var str = val.ToByteArray();
                writer.WriteBytes(str);
            }
            else if (ByteProto.TargetHasDescriptor(pType, out var objDesc) && objDesc != null)
            {
                WriteObject(objDesc, value, writer, textEncoding);
            }
            else if (pType.IsAssignableTo(ByteProto.ListType))
            {
                Type? itemType = null;
                if (value is IList collection)
                {
                    if (pType.IsGenericType && pType.GenericTypeArguments.Length > 0)
                    {
                        itemType = pType.GenericTypeArguments[0];
                    }
                    else if (pType.IsArray && !string.IsNullOrWhiteSpace(pType.FullName))
                    {
                        var fullName = pType.FullName[0..^2];
                        itemType = Type.GetType(fullName);
                    }

                    if (itemType == null)
                    {
                        throw new Exception($"Cannot resolve item type for target {pType}");
                    }

                    WriteCompressedSize(writer, collection.Count);
                    foreach (var val in collection)
                    {
                        WriteTypeValue(itemType, val, writer, textEncoding);
                    }
                }
                else
                {
                    throw new NotSupportedException($"target type {pType}, item type:{itemType} is not supported yet!");
                }
            }
            else
            {
                throw new NotSupportedException($"target type {pType} is not supported yet!");
            }
        }
    }
}
