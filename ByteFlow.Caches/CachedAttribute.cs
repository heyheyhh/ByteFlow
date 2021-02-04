using System;

namespace ByteFlow.Caches
{
    /// <summary>
    /// 表示被修饰的类型将用于存放至缓存
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CachedAttribute : Attribute
    {
        public CachedAttribute(CacheType cacheType)
        {
            this.CacheType = cacheType;
        }

        /// <summary>
        /// 用于指示此特性修饰的类型在缓存中应该如何存储
        /// </summary>
        public CacheType CacheType { get; private set; }

        /// <summary>
        /// 是否需要给Key加前缀，默认为 <see cref="string.Empty"/>
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// 如果指定了 <see cref="KeyPrefix"/>, 则会使用此分隔符分隔前缀与具体的 Key。
        /// 默认的分隔符为英文的冒号 ":"
        /// </summary>
        public char KeySeparator { get; set; } = ':';

        /// <summary>
        /// 当前类型使用的数据库，默认为0
        /// </summary>
        public int Database { get; set; } = 0;
    }

    public enum CacheType
    {
        /// <summary>
        /// 以 Hash 的方式存储。对象默认采用 Hash 的方式存储
        /// </summary>
        Hash = 0,

        /// <summary>
        /// 以 String 的方式存储
        /// </summary>
        String = 1,
    }
}
