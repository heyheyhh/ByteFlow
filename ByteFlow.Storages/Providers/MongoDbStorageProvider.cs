using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Polly;

namespace ByteFlow.Storages
{
    public sealed class MongoDbStorageProvider : BackgroundService, IStorageProvider
    {
        private MongoDbOptions Options => _optionsMonitor.CurrentValue;
        private int MaxRetryTimes => Options.MaxRetryTimes <= 0 ? 10 : Options.MaxRetryTimes;
        private int RetryDuration => Options.RetryDuration <= 0 ? 50 : Options.RetryDuration;
        
        private readonly Dictionary<Type, DocumentAttribute> _entityDocumentAttributes;
        private readonly IOptionsMonitor<MongoDbOptions> _optionsMonitor;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;

        public MongoDbStorageProvider(IOptionsMonitor<MongoDbOptions> options, Dictionary<Type, DocumentAttribute> entityDocumentAttributes)
        {
            _optionsMonitor = options;
            _entityDocumentAttributes = entityDocumentAttributes ?? throw new ArgumentNullException(nameof(entityDocumentAttributes));

            if (string.IsNullOrWhiteSpace(options.CurrentValue.ConnectionString))
            {
                throw new ArgumentException("ConnectionString 不能为空", nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.CurrentValue.Database))
            {
                throw new ArgumentException("Database 不能为空", nameof(options));
            }
            
            _client = new MongoClient(Options.ConnectionString);
            _database = _client.GetDatabase(Options.Database);
        }

        public async Task UseTransactionAsync(
            Func<TransactionContext, CancellationToken, Task> action,
            TransactionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var session = await _client.StartSessionAsync(null, cancellationToken);
            var context = new TransactionContext(session);

            // 开启事务
            session.StartTransaction(options);

            // 执行事务
            var executePolicy = Policy.Handle<MongoException>(ex => ex.HasErrorLabel("TransientTransactionError"))
                .WaitAndRetryAsync(MaxRetryTimes, retryAttempt => TimeSpan.FromMilliseconds(retryAttempt * RetryDuration));
            await executePolicy.ExecuteAsync(ct => action(context, ct), cancellationToken);

            // 提交事务
            if (session.IsInTransaction)
            {
                var commitPolicy = Policy.Handle<MongoException>(ex => ex.HasErrorLabel("UnknownTransactionCommitResult"))
                .WaitAndRetryAsync(MaxRetryTimes, retryAttempt => TimeSpan.FromMilliseconds(retryAttempt * RetryDuration));
                await commitPolicy.ExecuteAsync(() => session.CommitTransactionAsync(cancellationToken));
            }
        }

        public async Task<TRes> UseTransactionAsync<TRes>(
            Func<TransactionContext, CancellationToken, Task<TRes>> action,
            TransactionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var session = await _client.StartSessionAsync(null, cancellationToken);
            var context = new TransactionContext(session);

            // 开启事务
            session.StartTransaction(options);

            // 执行事务
            var executePolicy = Policy.Handle<MongoException>(ex => ex.HasErrorLabel("TransientTransactionError"))
                .WaitAndRetryAsync(MaxRetryTimes, retryAttempt => TimeSpan.FromMilliseconds(retryAttempt * RetryDuration));
            var res = await executePolicy.ExecuteAsync(ct => action(context, ct), cancellationToken);

            if (!session.IsInTransaction)
#pragma warning disable CS8603 // 可能的 null 引用返回。
                return default;
#pragma warning restore CS8603 // 可能的 null 引用返回。

                // 提交事务
            var commitPolicy = Policy.Handle<MongoException>(ex => ex.HasErrorLabel("UnknownTransactionCommitResult"))
                .WaitAndRetryAsync(MaxRetryTimes, retryAttempt => TimeSpan.FromMilliseconds(retryAttempt * RetryDuration));
            await commitPolicy.ExecuteAsync(() => context.Session.CommitTransactionAsync(cancellationToken));

            return res;

        }

        public Task AddOneAsync<TEntity>(
            TEntity entity,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var collection = EnsureCollection<TEntity>(executeOptions);
            return transactionContext != null ? collection.InsertOneAsync(transactionContext.Session, entity) : collection.InsertOneAsync(entity);
        }

        public Task AddManyAsync<TEntity>(
            IEnumerable<TEntity> entities,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var collection = EnsureCollection<TEntity>(executeOptions);
            return transactionContext != null ? collection.InsertManyAsync(transactionContext.Session, entities) : collection.InsertManyAsync(entities);
        }

        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            try
            {
                using var cusor = await this._client.ListDatabasesAsync();
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB 健康检查失败", ex);
            }
        }

        public async Task<bool> DeleteOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                var delResult = await collection.DeleteOneAsync(transactionContext.Session, filterDefinition);
                return delResult.IsAcknowledged && delResult.DeletedCount > 0;
            }
            else
            {
                var delResult = await collection.DeleteOneAsync(filterDefinition);
                return delResult.IsAcknowledged && delResult.DeletedCount > 0;
            }
        }

        public async Task<int> DeleteManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());

            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                var delResult = await collection.DeleteManyAsync(transactionContext.Session, filterDefinition);
                return delResult.IsAcknowledged ? (int)delResult.DeletedCount : 0;
            }
            else
            {
                var delResult = await collection.DeleteManyAsync(filterDefinition);
                return delResult.IsAcknowledged ? (int)delResult.DeletedCount : 0;
            }
        }

        public async Task<bool> ExistAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> condition,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = condition.Invoke(new FilterDefinitionBuilder<TEntity>());

            var options = new FindOptions<TEntity, TEntity>() { Limit = 1 };
            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                using var findResult = await collection.FindAsync(transactionContext.Session, filterDefinition, options);
                return await findResult.AnyAsync();
            }
            else
            {
                using var findResult = await collection.FindAsync(filterDefinition, options);
                return await findResult.AnyAsync();
            }
        }

        public async Task<TEntity?> FindOneAndUpdateAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> updateDefinitions,
            UpdateOptions? updateOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var update = updateDefinitions.Invoke(new UpdateDefinitionBuilder<TEntity>());
            var options = updateOptions == null ? null : new FindOneAndUpdateOptions<TEntity, TEntity>()
            {
                IsUpsert = updateOptions.IsUpsert,
                ReturnDocument = updateOptions.ReturnDocBeforeChange ? ReturnDocument.Before : ReturnDocument.After
            };
            var collection = EnsureCollection<TEntity>(executeOptions);

            if (transactionContext != null)
            {
                return await collection.FindOneAndUpdateAsync(transactionContext.Session, filterDefinition, update, options);
            }

            return await collection.FindOneAndUpdateAsync(filterDefinition, update, options);
        }

        public async Task<TEntity?> FindAndReplaceOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TEntity doc,
            UpdateOptions? updateOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var options = updateOptions == null ? null : new FindOneAndReplaceOptions<TEntity, TEntity>()
            {
                IsUpsert = updateOptions.IsUpsert,
                ReturnDocument = updateOptions.ReturnDocBeforeChange ? ReturnDocument.Before : ReturnDocument.After
            };
            var collection = EnsureCollection<TEntity>(executeOptions);

            if (transactionContext != null)
            {
                return await collection.FindOneAndReplaceAsync(transactionContext.Session, filterDefinition, doc, options);
            }

            return await collection.FindOneAndReplaceAsync(filterDefinition, doc, options);
        }

        public async Task<TEntity?> GetOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());

            var options = new FindOptions<TEntity, TEntity>() { Limit = 1 };
            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                using var findResult = await collection.FindAsync(transactionContext.Session, filterDefinition, options);
                return await findResult.FirstOrDefaultAsync();
            }
            else
            {
                using var findResult = await collection.FindAsync(filterDefinition, options);
                return await findResult.FirstOrDefaultAsync();
            }
        }

        public async Task<TEntity?> GetOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity>> outFieldsFilter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var projection = outFieldsFilter.Invoke(new ProjectionDefinitionBuilder<TEntity>());

            var options = new FindOptions<TEntity, TEntity>() { Projection = projection, Limit = 1 };
            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                using var findResult = await collection.FindAsync(transactionContext.Session, filterDefinition, options);
                return await findResult.FirstOrDefaultAsync();
            }
            else
            {
                using var findResult = await collection.FindAsync(filterDefinition, options);
                return await findResult.FirstOrDefaultAsync();
            }
        }

        public async Task<IEnumerable<TEntity>> GetManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<SortDefinitionBuilder<TEntity>, SortDefinition<TEntity>>? sort = null,
            GetManyOptions? options = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());

            var findOptions = new FindOptions<TEntity, TEntity>();
            var sortDefinition = sort?.Invoke(new SortDefinitionBuilder<TEntity>());
            if (sortDefinition != null)
            {
                findOptions.Sort = sortDefinition;
            }

            if (options != null)
            {
                findOptions.Skip = options.Skip >= 0 ? options.Skip : throw new ArgumentOutOfRangeException(nameof(options.Skip), "Skip must be equal or bigger than Zero");
                findOptions.Limit = options.Limit >= 0 ? options.Limit : throw new ArgumentOutOfRangeException(nameof(options.Limit), "Limit must equal or be bigger than Zero");
            }

            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                using var findResult = await collection.FindAsync(transactionContext.Session, filterDefinition, findOptions);
                return await findResult.ToListAsync();
            }
            else
            {
                using var findResult = await collection.FindAsync(filterDefinition, findOptions);
                return await findResult.ToListAsync();
            }
        }

        public async Task<IEnumerable<TEntity>> GetManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity>> outFieldsFilter,
            Func<SortDefinitionBuilder<TEntity>, SortDefinition<TEntity>>? sort = null,
            GetManyOptions? options = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var projection = outFieldsFilter.Invoke(new ProjectionDefinitionBuilder<TEntity>());

            var findOptions = new FindOptions<TEntity, TEntity>() { Projection = projection };
            var sortDefinition = sort?.Invoke(new SortDefinitionBuilder<TEntity>());
            if (sortDefinition != null)
            {
                findOptions.Sort = sortDefinition;
            }

            if (options != null)
            {
                findOptions.Skip = options.Skip >= 0 ? options.Skip : throw new ArgumentOutOfRangeException(nameof(options.Skip), "Skip must be equal or bigger than Zero");
                findOptions.Limit = options.Limit >= 0 ? options.Limit : throw new ArgumentOutOfRangeException(nameof(options.Limit), "Limit must be equal or bigger than Zero");
            }

            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                using var findResult = await collection.FindAsync(transactionContext.Session, filterDefinition, findOptions);
                return await findResult.ToListAsync();
            }
            else
            {
                using var findResult = await collection.FindAsync(filterDefinition, findOptions);
                return await findResult.ToListAsync();
            }
        }

        public async Task<bool> UpdateOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> updateDefinitions,
            UpdateOptions? updateOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var update = updateDefinitions.Invoke(new UpdateDefinitionBuilder<TEntity>());

            var options = updateOptions == null ? null : new MongoDB.Driver.UpdateOptions() { IsUpsert = updateOptions.IsUpsert };
            var collection = this.EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                var res = await collection.UpdateOneAsync(transactionContext.Session, filterDefinition, update, options);
                return res != null && res.IsModifiedCountAvailable && res.ModifiedCount > 0;
            }
            else
            {
                var res = await collection.UpdateOneAsync(filterDefinition, update, options);
                return res != null && res.IsModifiedCountAvailable && res.ModifiedCount > 0;
            }
        }

        public async Task<long> UpdateManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> updateDefinitions,
            UpdateManyOptions? updateManyOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class
        {
            var filterDefinition = filter.Invoke(new FilterDefinitionBuilder<TEntity>());
            var update = updateDefinitions.Invoke(new UpdateDefinitionBuilder<TEntity>());

            var options = updateManyOptions == null ? null : new MongoDB.Driver.UpdateOptions() { IsUpsert = updateManyOptions.IsUpsert };
            var collection = EnsureCollection<TEntity>(executeOptions);
            if (transactionContext != null)
            {
                var res = await collection.UpdateManyAsync(transactionContext.Session, filterDefinition, update, options);
                return res != null && res.IsModifiedCountAvailable ? res.ModifiedCount : 0;
            }
            else
            {
                var res = await collection.UpdateManyAsync(filterDefinition, update, options);
                return res != null && res.IsModifiedCountAvailable ? res.ModifiedCount : 0;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
#if DEBUG
            Console.WriteLine("+++++++++++++++++++++++++++开始初始化集合，确保集合存在+++++++++++++++++++");
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            // 确保各个集合均被创建
            using var namesCursor = await this._database.ListCollectionNamesAsync(null, stoppingToken);
            var names = await namesCursor.ToListAsync(stoppingToken);

            foreach (var (_, value) in this._entityDocumentAttributes)
            {
                try
                {
                    if (names.Contains(value.CollectionName))
                    {
                        Console.WriteLine($"集合 {value.CollectionName} 已存在, 无需创建");
                        continue;
                    }

                    Console.WriteLine($"准备创建集合 {value.CollectionName}");
                    await this._database.CreateCollectionAsync(value.CollectionName, null, stoppingToken);
                }
                catch (MongoException ex)
                {
                    // 如果集合已经存在，则会抛出异常，忽略即可
                    Console.WriteLine($"集合 {value.CollectionName} 创建失败, ex: {0}", ex);
                }
            }

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine($"+++++++++++++++++++++++++++完成集合的初始化, 花费时间: {stopwatch.ElapsedMilliseconds}ms +++++++++++++++++++"); 
#endif
        }

        private IMongoCollection<TEntity> EnsureCollection<TEntity>(ExecuteOptions? executeOptions)
        {
            if (executeOptions != null)
            {
                return _client.GetDatabase(executeOptions.DatabaseName).GetCollection<TEntity>(executeOptions.CollectionName);
            }

            var docAttr = _entityDocumentAttributes[typeof(TEntity)];
            return _database.GetCollection<TEntity>(docAttr.CollectionName);
        }
    }
}