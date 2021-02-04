using ByteFlow.Streams.Abstractions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace ByteFlow.Streams
{
    public sealed class BytesWriter : IBytesWriter
    {
        public const int BufferSpanSize = 512;

        public int Position { get; set; }

        public ReadOnlySpan<byte> Bytes => new ReadOnlySpan<byte>(_buffer, 0, this.Position);

        public Endian Endian { get; }

        private bool IsBigEndian { get; }

        private byte[] _buffer;
        private bool _disposed;

        public BytesWriter(Endian endian = Endian.BIG_ENDIAN)
        {
            this._buffer = ArrayPool<byte>.Shared.Rent(BufferSpanSize);// new byte[BufferSpanSize];
            this.Endian = endian;
            this.IsBigEndian = endian == Endian.BIG_ENDIAN;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BytesWriter()
        {
            Dispose(false);
        }

        //这里的参数表示示是否需要释放那些实现IDisposable接口的托管对象
        private void Dispose(bool disposing)
        {
            if (_disposed) return; //如果已经被回收，就中断执行
            if (disposing)
            {
                // 释放那些实现IDisposable接口的托管对象
            }

            // 释放非托管资源，设置对象为null
            ArrayPool<byte>.Shared.Return(this._buffer, true);
            this._buffer = Array.Empty<byte>();

            _disposed = true;
        }


        public void WriteBool(bool val) => this.WriteByte(val ? 1 : 0);

        public void WriteByte(byte val) => this.WriteBytes(new[] { val }, 0, 1);

        public void WriteShort(short val)
        {
            if (IsBigEndian)
            {
                this.Write(2, buf => BinaryPrimitives.WriteInt16BigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(2, buf => BinaryPrimitives.WriteInt16LittleEndian(new Span<byte>(buf), val));
        }

        public void WriteUShort(ushort val)
        {
            if (IsBigEndian)
            {
                this.Write(2, buf => BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(2, buf => BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buf), val));
        }

        public void WriteInt(int val)
        {
            if (IsBigEndian)
            {
                this.Write(4, buf => BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(4, buf => BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(buf), val));
        }

        public void WriteUInt(uint val)
        {
            if (IsBigEndian)
            {
                this.Write(4, buf => BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(4, buf => BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buf), val));
        }

        public void WriteLong(long val)
        {
            if (IsBigEndian)
            {
                this.Write(8, buf => BinaryPrimitives.WriteInt64BigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(8, buf => BinaryPrimitives.WriteInt64LittleEndian(new Span<byte>(buf), val));
        }

        public void WriteULong(ulong val)
        {
            if (IsBigEndian)
            {
                this.Write(8, buf => BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(8, buf => BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(buf), val));
        }

        public void WriteDouble(double val)
        {
            if (IsBigEndian)
            {
                this.Write(8, buf => BinaryPrimitives.WriteDoubleBigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(8, buf => BinaryPrimitives.WriteDoubleLittleEndian(new Span<byte>(buf), val));
        }

        public void WriteFloat(float val)
        {
            if (IsBigEndian)
            {
                this.Write(4, buf => BinaryPrimitives.WriteSingleBigEndian(new Span<byte>(buf), val));
                return;
            }
            this.Write(4, buf => BinaryPrimitives.WriteSingleLittleEndian(new Span<byte>(buf), val));
        }

        public void WriteBytes(byte[] bytes) => this.WriteBytes(bytes, 0, bytes.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] bytes, int offset, int count)
        {
            if (count <= 0)
            {
                return;
            }

            this.EnsureBuffer(count);

            var span = new Span<byte>(bytes, offset, count);
            var dstSpan = new Span<byte>(this._buffer, this.Position, count);
            span.CopyTo(dstSpan);
            Position += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(Span<byte> bytes)
        {
            if (bytes.Length <= 0)
            {
                return;
            }

            this.EnsureBuffer(bytes.Length);

            var dstSpan = new Span<byte>(this._buffer, this.Position, bytes.Length);
            bytes.CopyTo(dstSpan);
            Position += bytes.Length;
        }

        public void WriteString(string val, Encoding? encoding = null)
        {
            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var encode = encoding ?? Encoding.UTF8;
            var bytes = encode.GetBytes(val);
            this.WriteBytes(bytes);
        }

        public void WriteFixLengthString(string val, int fixLength, Encoding? encoding = null)
        {
            if (string.IsNullOrEmpty(val))
            {
                return;
            }
            if (fixLength <= 0)
            {
                throw new ArgumentException($"{nameof(fixLength)} cannot smaller than zero.");
            }

            var srcBytes = (encoding ?? Encoding.UTF8).GetBytes(val);
            int dstSize = Math.Min(srcBytes.Length, fixLength);
            var srcSpan = new Span<byte>(srcBytes, 0, dstSize);

            var dstSpan = new Span<byte>(new byte[fixLength]);
            dstSpan.Fill(0);

            srcSpan.CopyTo(dstSpan);

            this.WriteBytes(dstSpan);
        }

        public void WriteAt(int pos, Action<IBytesWriter> writer)
        {
            var save = this.Position;
            this.Position = pos;
            writer(this);
            this.Position = save;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(int bytesCount, Action<byte[]> write)
        {
            this.EnsureBuffer(bytesCount);

            var tmpBuf = ArrayPool<byte>.Shared.Rent(bytesCount);
            try
            {
                write(tmpBuf);
                this.WriteBytes(tmpBuf, 0, bytesCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tmpBuf, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBuffer(int targetBytesCount)
        {
            if (this.Position + targetBytesCount <= this._buffer.Length)
            {
                return;
            }

            var oldBuffer = this._buffer;
            try
            {
                var bufSize = (int)Math.Ceiling((oldBuffer.Length + targetBytesCount) / (double)BufferSpanSize) * BufferSpanSize;
                this._buffer = ArrayPool<byte>.Shared.Rent(bufSize);
                Buffer.BlockCopy(oldBuffer, 0, this._buffer, 0, oldBuffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(oldBuffer, true);
            }
        }
    }
}
