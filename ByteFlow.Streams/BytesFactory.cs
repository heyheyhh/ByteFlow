using ByteFlow.Streams.Abstractions;

namespace ByteFlow.Streams
{
    public static class BytesFactory
    {
        /// <summary>
        /// 用于创建默认的字节流读取程序
        /// </summary>
        /// <param name="bytes">待读取的字节流</param>
        /// <param name="endian">字节顺序，默认为<see cref="Endian.BIG_ENDIAN"/></param>
        /// <returns>字节流读取程序</returns>
        public static IBytesReader GetReader(byte[] bytes, Endian endian = Endian.BIG_ENDIAN) => new BytesReader(bytes, endian);

        /// <summary>
        /// 用于创建默认的字节流写入程序
        /// </summary>
        /// <returns>字节流写入程序</returns>
        /// <param name="endian">字节顺序，默认为<see cref="Endian.BIG_ENDIAN"/></param>
        public static IBytesWriter GetWriter(Endian endian = Endian.BIG_ENDIAN) => new BytesWriter(endian);
    }
}
