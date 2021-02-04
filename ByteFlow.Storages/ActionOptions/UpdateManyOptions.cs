namespace ByteFlow.Storages
{
    /// <summary>
    /// 更新选项
    /// </summary>
    public class UpdateManyOptions
    {
        /// <summary>
        /// 当待更新的文档不存在时，是否插入该文档
        /// </summary>
        public bool IsUpsert { get; set; }
    }
}