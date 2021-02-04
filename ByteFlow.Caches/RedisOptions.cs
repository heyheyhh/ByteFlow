using System.Collections.Generic;

namespace ByteFlow.Caches
{
    public class RedisOptions
    {
        /// <summary>
        /// 配置字符串
        /// </summary>
        public string Configuration { get; set; } = string.Empty;

        /// <summary>
        /// 允许操作的数据库
        /// </summary>
        public List<int> AllowedDatabases { get; set; } = new List<int>();
    }
}
