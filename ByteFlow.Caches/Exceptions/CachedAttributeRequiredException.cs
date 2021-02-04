using ByteFlow.Exceptions;
using System;

namespace ByteFlow.Caches
{
    /// <summary>
    /// 当被缓存对象没有被 <see cref="CachedAttribute"/> 标记时，抛出此异常
    /// </summary>
    public class CachedAttributeRequiredException : ExceptionAbstract
    {
        public override string IntentMessage => $"该缓存对象缺少 {nameof(CachedAttribute)} 特性";

        public CachedAttributeRequiredException(int skipFrames = 0)
               : base(string.Empty, skipFrames)
        {
        }

        public CachedAttributeRequiredException(string message, int skipFrames = 0)
            : base(message, skipFrames)
        {
        }

        public CachedAttributeRequiredException(string message, Exception innerException, int skipFrames = 0)
            : base(message, innerException, skipFrames)
        {
        }
    }
}
