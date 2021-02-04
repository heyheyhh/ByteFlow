using System;
using System.Text;

namespace ByteFlow.Streams.Abstractions
{
    public interface IBytesReader
    {
        int Position { get; set; }

        ReadOnlySpan<byte> Data { get; }

        Endian Endian { get; }

        int ByteAvailable => this.Data.Length - this.Position;

        bool ReadBool();

        byte ReadByte();

        short ReadShort();

        ushort ReadUShort();

        int ReadInt();

        uint ReadUInt();

        long ReadLong();

        ulong ReadULong();

        double ReadDouble();

        float ReadFloat();

        string ReadString(int lengthInBytes, Encoding? encoding = null);

        ReadOnlySpan<byte> ReadBytes(int bytesCount);
    }
}
