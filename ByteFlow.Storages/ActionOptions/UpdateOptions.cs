namespace ByteFlow.Storages
{
    /// <summary>
    /// 更新选项
    /// </summary>
    public class UpdateOptions
    {
        /// <summary>
        /// 当待更新的文档不存在时，是否插入该文档
        /// </summary>
        public bool IsUpsert { get; set; }

        /// <summary>
        /// 当为True时，返回更新之前的文档；否则，返回更新之后的文档
        /// </summary>
        public bool ReturnDocBeforeChange { get; set; }
    }
}