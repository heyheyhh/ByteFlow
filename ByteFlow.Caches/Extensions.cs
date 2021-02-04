using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ByteFlow.Caches
{
    public static class Extensions
    {
        /// <summary>
        /// 使用 Redis 来提供缓存处理能力
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="config">Redis配置信息</param>
        public static IServiceCollection AddRedisCacheProvider(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<RedisOptions>(config);
            services.AddSingleton<RedisCacheProvider>();
            services.AddSingleton<ICacheProvider>(sp => sp.GetRequiredService<RedisCacheProvider>());
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RedisCacheProvider>());

            return services;
        }

        /// <summary>
        /// 添加 CacheProvider 健康检查
        /// </summary>
        /// <param name="builder">健康检查程序集合</param>
        public static IHealthChecksBuilder AddCacheProviderHealthCheck(this IHealthChecksBuilder builder)
            => builder.AddCheck<CacheProviderHealthCheck>("CacheProvider");
    }
}
