namespace ByteFlow.Storages
{
    /// <summary>
    /// 数据库操作执行选项
    /// </summary>
    public class ExecuteOptions
    {
        /// <summary>
        /// 数据存储的集合的名称
        /// </summary>
        public string CollectionName { get; }

        /// <summary>
        /// 指定文档存储的数据库。如果不指定，则采用全局默认配置的数据库
        /// </summary>
        public string DatabaseName { get; }

        public ExecuteOptions(string collection, string database)
        {
            this.CollectionName = collection;
            this.DatabaseName = database;
        }
    }
}