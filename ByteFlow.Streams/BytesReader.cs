using ByteFlow.Streams.Abstractions;
using System;
using System.Buffers.Binary;
using System.Text;

namespace ByteFlow.Streams
{
    public sealed class BytesReader : IBytesReader
    {
        public int Position { get; set; }

        public ReadOnlySpan<byte> Data => _bytes;

        public Endian Endian { get; }

        private bool IsBigEndian { get; }

        private readonly byte[] _bytes;

        public BytesReader(byte[] bytes, Endian endian = Endian.BIG_ENDIAN)
        {
            this._bytes = bytes;
            this.Endian = endian;
            this.IsBigEndian = endian == Endian.BIG_ENDIAN;            
        }

        public bool ReadBool()
        {
            var bt = this._bytes[this.Position];
            this.Position += 1;
            return bt != 0;
        }

        public byte ReadByte()
        {
            var bt = this._bytes[this.Position];
            this.Position += 1;
            return bt;
        }

        public short ReadShort() => IsBigEndian ? BinaryPrimitives.ReadInt16BigEndian(Slice(2)) : BinaryPrimitives.ReadInt16LittleEndian(Slice(2));

        public ushort ReadUShort() => IsBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(Slice(2)) : BinaryPrimitives.ReadUInt16LittleEndian(Slice(2));

        public int ReadInt() => IsBigEndian ? BinaryPrimitives.ReadInt32BigEndian(Slice(4)) : BinaryPrimitives.ReadInt32LittleEndian(Slice(4));

        public uint ReadUInt() => IsBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(Slice(4)) : BinaryPrimitives.ReadUInt32LittleEndian(Slice(4));

        public long ReadLong() => IsBigEndian ? BinaryPrimitives.ReadInt64BigEndian(Slice(8)) : BinaryPrimitives.ReadInt64LittleEndian(Slice(8));

        public ulong ReadULong() => IsBigEndian ? BinaryPrimitives.ReadUInt64BigEndian(Slice(8)) : BinaryPrimitives.ReadUInt64LittleEndian(Slice(8));

        public double ReadDouble() => IsBigEndian ? BinaryPrimitives.ReadDoubleBigEndian(Slice(8)) : BinaryPrimitives.ReadDoubleLittleEndian(Slice(8));

        public float ReadFloat() => IsBigEndian ? BinaryPrimitives.ReadSingleBigEndian(Slice(4)) : BinaryPrimitives.ReadSingleLittleEndian(Slice(4));

        public string ReadString(int lengthInBytes, Encoding? encoding = null) => lengthInBytes > 0 ? (encoding ?? Encoding.UTF8).GetString(Slice(lengthInBytes)).Trim('\0') : string.Empty;

        public ReadOnlySpan<byte> ReadBytes(int bytesCount) => Slice(bytesCount);

        private ReadOnlySpan<byte> Slice(int bytesCount)
        {
            var span = new ReadOnlySpan<byte>(this._bytes, this.Position, bytesCount);
            this.Position += bytesCount;
            return span;
        }
    }
}
