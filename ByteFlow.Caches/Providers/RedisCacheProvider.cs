using ByteFlow.Serializers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ByteFlow.Caches
{
    public class RedisCacheProvider : BackgroundService, ICacheProvider
    {
        private const string HealthCheckKey = "cache_provider_healthCheck";

        private readonly object _connectionMultiplexerLocker = new object();

        private RedisOptions Options => _optionsMonitor.CurrentValue;
        private readonly IJsonSerializer _serializer;

        private readonly ConcurrentDictionary<Type, CachedAttribute> _typeAttr = new();

        private ConnectionMultiplexer? _connectionMultiplexer;
        private readonly IOptionsMonitor<RedisOptions> _optionsMonitor;

        public RedisCacheProvider(IOptionsMonitor<RedisOptions> options, IJsonSerializer? serializer)
        {
            _optionsMonitor = options;
            var config = options.CurrentValue;
            if (string.IsNullOrWhiteSpace(config.Configuration))
            {
                throw new ArgumentException("值不能为空.", nameof(options));
            }

            if (config.AllowedDatabases == null || config.AllowedDatabases.Count <= 0)
            {
                throw new ArgumentException("AllowedDatabases 不能为空", nameof(options));
            }

            this._serializer = serializer ?? SerializerFactory.GetTextJsonSerializer();
        }

        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            try
            {
                var dbIndex = this.Options.AllowedDatabases.Count > 0 ? this.Options.AllowedDatabases[0] : 0;
                var db = this.GetDatabase(dbIndex);
                await db.StringSetAsync(HealthCheckKey, $"Health Check at {DateTimeOffset.Now}", TimeSpan.FromSeconds(30));
                await db.StringGetAsync(HealthCheckKey);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Redis健康检查失败", ex);
            }

            return HealthCheckResult.Healthy();
        }

        public async Task<bool> DeleteAsync<TEntity>(string key)
            where TEntity : class, new()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key不能为空", nameof(key));
            }

            var attr = this.GetCachedAttribute<TEntity>();
            if (attr == null)
            {
                throw new CachedAttributeRequiredException();
            }

            var db = this.GetDatabase(attr.Database);
            var realKey = EvaluateRealKey(attr, key);

            return await db.KeyDeleteAsync(realKey);
        }

        public Task<bool> ExtendExpiryAsync<TEntity>(string key, TimeSpan expiry)
            where TEntity : class, new()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key不能为空", nameof(key));
            }

            var attr = this.GetCachedAttribute<TEntity>();
            if (attr == null)
            {
                throw new CachedAttributeRequiredException();
            }

            var db = this.GetDatabase(attr.Database);
            var realKey = EvaluateRealKey(attr, key);

            return db.KeyExpireAsync(realKey, expiry);
        }

        public async Task<TEntity?> GetAsync<TEntity>(string key, TimeSpan? expiry = null)
            where TEntity : class, new()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key不能为空", nameof(key));
            }

            var attr = this.GetCachedAttribute<TEntity>();
            if (attr == null)
            {
                throw new CachedAttributeRequiredException();
            }

            var db = this.GetDatabase(attr.Database);
            var realKey = EvaluateRealKey(attr, key);
            if (attr.CacheType == CacheType.String)
            {
                if (expiry != null)
                {
                    // 使用 Redis 事务来保证顺序
                    var transaction = db.CreateTransaction();
                    var jsonTask = transaction.StringGetAsync(realKey);
                    var expiryTask = transaction.KeyExpireAsync(realKey, expiry);
                    await transaction.ExecuteAsync();
                    await Task.WhenAll(jsonTask, expiryTask);
                    return this.Map<TEntity>(jsonTask.Result);
                }
                else
                {
                    var json = await db.StringGetAsync(realKey);
                    return this.Map<TEntity>(json);
                }
            }
            else if (attr.CacheType == CacheType.Hash)
            {
                if (expiry != null)
                {
                    // 使用 Redis 事务来保证顺序
                    var transaction = db.CreateTransaction();
                    var entriesTask = transaction.HashGetAllAsync(realKey);
                    var expiryTask = transaction.KeyExpireAsync(realKey, expiry);
                    await transaction.ExecuteAsync();
                    await Task.WhenAll(entriesTask, expiryTask);
                    return Map<TEntity>(entriesTask.Result);
                }
                else
                {
                    var entries = await db.HashGetAllAsync(realKey);
                    return Map<TEntity>(entries);
                }
            }

            throw new NotSupportedException($"现在还不支持 CacheType:{attr.CacheType}");
        }

        public async Task<bool> SetAsync<TEntity>(string key, TEntity entity, TimeSpan expiry)
            where TEntity : class, new()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key不能为空", nameof(key));
            }

            var attr = this.GetCachedAttribute<TEntity>();
            if (attr == null)
            {
                throw new CachedAttributeRequiredException();
            }

            var db = this.GetDatabase(attr.Database);
            var realKey = EvaluateRealKey(attr, key);
            switch (attr.CacheType)
            {
                case CacheType.String:
                {
                    var json = this._serializer.Serialize(entity);
                    return await db.StringSetAsync(realKey, json, expiry);
                }
                case CacheType.Hash:
                {
                    var entries = Map(entity);

                    // 此处需要用 Redis 事务来保证顺序。这样才能保证过期时间正确设置
                    var transaction = db.CreateTransaction();
                    var setTask = transaction.HashSetAsync(realKey, entries);
                    var expiryTask = transaction.KeyExpireAsync(realKey, expiry);
                    await transaction.ExecuteAsync();
                    await Task.WhenAll(setTask, expiryTask);
                    return expiryTask.Result;
                }
                default:
                    throw new NotSupportedException($"现在还不支持 CacheType:{attr.CacheType}");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (this._connectionMultiplexer != null)
                {
                    await this._connectionMultiplexer.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                // Ignore
                Console.WriteLine(ex);
            }
            finally
            {
                this._connectionMultiplexer = null;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.StopAsync(CancellationToken.None);

            this._connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(this.Options.Configuration);
        }

        private IDatabase GetDatabase(int database)
        {
            if (!this.Options.AllowedDatabases.Contains(database))
            {
                throw new NotAllowedRedisDatabaseException() { DesiredDatabases = this.Options.AllowedDatabases };
            }

            if (this._connectionMultiplexer != null && this._connectionMultiplexer.IsConnected) 
                return this._connectionMultiplexer.GetDatabase(database);
            
            lock (this._connectionMultiplexerLocker)
            {
                if (this._connectionMultiplexer != null && this._connectionMultiplexer.IsConnected) 
                    return this._connectionMultiplexer.GetDatabase(database);
                try
                {
                    this._connectionMultiplexer?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    this._connectionMultiplexer = null;
                }

                this._connectionMultiplexer = ConnectionMultiplexer.Connect(this.Options.Configuration);
            }

            return this._connectionMultiplexer.GetDatabase(database);
        }

        private CachedAttribute? GetCachedAttribute<TEntity>()
            where TEntity : class, new()
        {
            var type = typeof(TEntity);
            if (this._typeAttr.TryGetValue(type, out var objectAttribute))
            {
                return objectAttribute;
            }

            var attr = type.GetCustomAttribute<CachedAttribute>();
            if (attr != null)
            {
                this._typeAttr.TryAdd(type, attr);
            }

            return attr;
        }

        private TEntity? Map<TEntity>(string value)
            where TEntity : class, new()
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return this._serializer.Deserialize<TEntity>(value);
        }

        private static TEntity? Map<TEntity>(IReadOnlyCollection<HashEntry> entries)
            where TEntity : class, new()
        {
            if (entries.Count <= 0)
            {
                return default;
            }

            var type = typeof(TEntity);
            var properties = type.GetProperties();
            if (properties.Length <= 0)
            {
                return default;
            }

            var entity = new TEntity();
            foreach (var p in properties)
            {
                var value = entries.FirstOrDefault(h => h.Name == p.Name);
                if (p.PropertyType.BaseType == typeof(Enum))
                {
                    var obj = Enum.Parse(p.PropertyType, value.Value, true);
                    p.SetValue(entity, obj);
                }
                else
                {
                    p.SetValue(entity, Convert.ChangeType(value.Value, p.PropertyType));
                }
            }

            return entity;
        }

        private static HashEntry[] Map<TEntity>(TEntity entity)
            where TEntity : class, new()
        {
            var type = typeof(TEntity);
            var properties = type.GetProperties();
            if (properties.Length <= 0)
            {
                return Array.Empty<HashEntry>();
            }

            var entries = new HashEntry[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                var p = properties[i];
                var value = p.GetValue(entity);
                entries[i] = new HashEntry(p.Name, value != null ? value.ToString() : string.Empty);
            }

            return entries;
        }

        private static string EvaluateRealKey(CachedAttribute attr, string key)
            => string.IsNullOrWhiteSpace(attr.KeyPrefix) ? key : $"{attr.KeyPrefix}{attr.KeySeparator}{key}";
    }
}
