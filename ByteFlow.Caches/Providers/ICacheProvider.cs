using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;

namespace ByteFlow.Caches
{
    /// <summary>
    /// 缓存提供程序
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// 设置指定 key 的值
        /// </summary>
        /// <typeparam name="TEntity">存储在该key中的对象的类型</typeparam>
        /// <param name="key">存储的key</param>
        /// <param name="entity">存储在该key中的对象</param>
        /// <param name="expiry">过期时间</param>
        Task<bool> SetAsync<TEntity>(string key, TEntity entity, TimeSpan expiry)
            where TEntity : class, new();

        /// <summary>
        /// 获取指定 key 中存储的对象
        /// </summary>
        /// <typeparam name="TEntity">存储在该key中的对象的类型</typeparam>
        /// <param name="key">存储的key</param>
        /// <param name="expiry">是否在获取的同时更新该key的过期时间</param>
        Task<TEntity?> GetAsync<TEntity>(string key, TimeSpan? expiry = null)
            where TEntity : class, new();

        /// <summary>
        /// 删除指定的key
        /// </summary>
        /// <typeparam name="TEntity">存储在该key中的对象的类型</typeparam>
        /// <param name="key">待删除的key</param>
        Task<bool> DeleteAsync<TEntity>(string key)
            where TEntity : class, new();

        /// <summary>
        /// 延长指定key的过期时间
        /// </summary>
        /// <typeparam name="TEntity">存储在该key中的对象的类型</typeparam>
        /// <param name="key">待操作的key</param>
        /// <param name="expiry">过期时间</param>
        Task<bool> ExtendExpiryAsync<TEntity>(string key, TimeSpan expiry)
            where TEntity : class, new();

        /// <summary>
        /// 用于检查当前 CacheProvider 是否健康
        /// </summary>
        Task<HealthCheckResult> CheckHealthAsync();
    }
}
