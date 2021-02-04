using System;

namespace ByteFlow.Extensions
{
    public static class TimeSpanExtension
    {
        /// <summary>
        /// 将指定的 span 格式化为 hh:mm:ss 的字符串
        /// </summary>
        /// <returns>hh:mm:ss 格式的字符串</returns>
        public static string ToHHMMSS(this TimeSpan span) => span.ToString(@"hh\:mm\:ss");
    }
}
