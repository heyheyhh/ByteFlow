namespace ByteFlow.Storages
{
    /// <summary>
    /// 当调用一次性获取多条数据时，采用的配置项
    /// </summary>
    public class GetManyOptions
    {
        /// <summary>
        /// 跳过多少记录开始返回结果
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// 允许返回的结果的数量
        /// </summary>
        public int Limit { get; set; }
    }
}