namespace ByteFlow.Storages
{
    /// <summary>
    /// MongoDB 配置信息
    /// </summary>
    public class MongoDbOptions
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 平台数据库名称
        /// </summary>
        public string Database { get; set; } = string.Empty;
        
        /// <summary>
        /// 失败可重试的次数，默认为 10 次
        /// </summary>
        public int MaxRetryTimes { get; set; }
        
        /// <summary>
        /// 每次重试之间的间隔时间（毫秒），默认为 50ms.
        /// 重试机制如下：
        /// 第一次重试间隔为 50ms（1 * 50ms），
        /// 第二次重试间隔为 100ms（2 * 50ms），
        /// 第三次重试间隔为 150ms（3 * 50ms），
        /// ...
        /// 第十次重试间隔为 500ms（10 * 50ms），
        /// </summary>
        public int RetryDuration { get; set; }
    }
}