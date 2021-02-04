namespace ByteFlow.WebSockets
{
    public enum ConnectionMessageType
    {
        /// <summary>
        /// 未知格式
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 内容为字节流
        /// </summary>
        Binary = 1,
        /// <summary>
        /// 内容为文本
        /// </summary>
        Text = 2,
    }
}
