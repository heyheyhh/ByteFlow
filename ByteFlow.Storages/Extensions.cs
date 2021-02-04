using System;
using System.Collections.Generic;
using System.Reflection;
using ByteFlow.Storages.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;

namespace ByteFlow.Storages
{
    public static class Extensions
    {
        /// <summary>
        /// 添加 MongoDB StorageProvider
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="config">MongoDB Options 配置信息</param>
        /// <param name="mongoEntityAssemblies">数据库实体类型所在程序集</param>
        public static IServiceCollection AddMongoDbStorageProvider(this IServiceCollection services, IConfiguration config, params Assembly[] mongoEntityAssemblies)
        {
            BsonSerializer.RegisterSerializationProvider(new CustomBsonSerializationProvider());

            services.Configure<MongoDbOptions>(config);
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<MongoDbOptions>>();
                var entryAssm = Assembly.GetEntryAssembly();
                var assemblies = mongoEntityAssemblies.Length <= 0 && entryAssm != null ? new[] { entryAssm } : mongoEntityAssemblies;

                // 在程序集中找到所有有 <see cref="DocumentAttribute"/> 的类型
                var entityDocumentAttributes = new Dictionary<Type, DocumentAttribute>();
                foreach (var assembly in assemblies)
                {
                    var types = assembly.ExportedTypes;
                    foreach (var type in types)
                    {
                        if (!type.IsClass || type.IsAbstract)
                        {
                            continue;
                        }

                        var docAttr = type.GetCustomAttribute<DocumentAttribute>();
                        if (docAttr == null)
                        {
                            continue;
                        }

                        entityDocumentAttributes.Add(type, docAttr);
                    }
                }

                return new MongoDbStorageProvider(options, entityDocumentAttributes);
            });
            services.AddSingleton<IStorageProvider>(sp => sp.GetRequiredService<MongoDbStorageProvider>());
            services.AddHostedService(sp => sp.GetRequiredService<MongoDbStorageProvider>());

            return services;
        }

        /// <summary>
        /// 添加 StorageProvider 健康检查
        /// </summary>
        /// <param name="builder">健康检查程序集合</param>
        public static IHealthChecksBuilder AddStorageProviderHealthCheck(this IHealthChecksBuilder builder)
            => builder.AddCheck<StorageProviderHealthCheck>("StorageProvider");
    }
}