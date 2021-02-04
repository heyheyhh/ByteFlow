using ByteFlow.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ByteFlow.Caches
{
    /// <summary>
    /// 当在获取Redis数据库实例的时候，如果不是服务指定的数据库，则抛出此异常
    /// </summary>
    public class NotAllowedRedisDatabaseException : ExceptionAbstract
    {
        /// <summary>
        /// 期望的 key 的类型
        /// </summary>
        public IReadOnlyList<int> DesiredDatabases { get; set; } = Array.Empty<int>();

        public NotAllowedRedisDatabaseException(int skipFrames = 0)
               : base(skipFrames)
        {
        }

        public NotAllowedRedisDatabaseException(string message, int skipFrames = 0)
            : base(message, skipFrames)
        {
        }

        public NotAllowedRedisDatabaseException(string message, Exception innerException, int skipFrames = 0)
            : base(message, innerException, skipFrames)
        {
        }

        protected override void BuildString(StringBuilder stringBuilder)
        {
            base.BuildString(stringBuilder);
            if (this.DesiredDatabases != null)
            {
                string databases = string.Join(",", this.DesiredDatabases);
                stringBuilder.AppendLine($"desiredDatabases: {databases}");
            }
        }
    }
}
