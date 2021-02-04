using System;
using System.Text;

namespace ByteFlow.Streams.Abstractions
{
    public interface IBytesWriter : IDisposable
    {
        int Position { get; set; }

        ReadOnlySpan<byte> Bytes { get; }

        Endian Endian { get; }

        void WriteBool(bool val);

        void WriteByte(byte val);

        void WriteShort(short val);

        void WriteUShort(ushort val);

        void WriteInt(int val);

        void WriteUInt(uint val);

        void WriteLong(long val);

        void WriteULong(ulong val);

        void WriteDouble(double val);

        void WriteFloat(float val);

        void WriteBytes(byte[] bytes);

        void WriteBytes(byte[] bytes, int offset, int count);

        void WriteBytes(Span<byte> bytes);

        void WriteString(string val, Encoding? encoding = null);

        void WriteFixLengthString(string val, int fixLength, Encoding? encoding = null);

        void WriteAt(int pos, Action<IBytesWriter> writer);
    }
}
