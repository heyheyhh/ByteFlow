using System;

namespace ByteFlow.Storages
{
    /// <summary>
    /// 用于描述一个数据库实体
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DocumentAttribute : Attribute
    {
        /// <summary>
        /// 初始化描述信息
        /// </summary>
        /// <param name="collectionName">该实体所存储的集合的名称</param>
        public DocumentAttribute(string collectionName)
        {
            this.CollectionName = collectionName;
        }

        /// <summary>
        /// 数据存储的集合的名称
        /// </summary>
        public string CollectionName { get; private set; }
    }
}
