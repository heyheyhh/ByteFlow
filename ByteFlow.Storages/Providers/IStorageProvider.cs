using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace ByteFlow.Storages
{
    /// <summary>
    /// 存储（读写）机制提供程序
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// 通过事务执行相关操作
        /// </summary>
        /// <param name="action">在此事务中执行的操作</param>
        /// <param name="options">事务选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task UseTransactionAsync(
            Func<TransactionContext, CancellationToken, Task> action,
            TransactionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 通过事务执行相关操作
        /// </summary>
        /// <typeparam name="TRes">返回的结果的类型</typeparam>
        /// <param name="action">在此事务中执行的操作</param>
        /// <param name="options">事务选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>事务执行结果</returns>
        Task<TRes> UseTransactionAsync<TRes>(
            Func<TransactionContext, CancellationToken, Task<TRes>> action,
            TransactionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 用于检查当前存储器是否健康
        /// </summary>
        Task<HealthCheckResult> CheckHealthAsync();

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="entity">实体实例</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        Task AddOneAsync<TEntity>(TEntity entity, TransactionContext? transactionContext = null, ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="entities">实体实例</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        Task AddManyAsync<TEntity>(IEnumerable<TEntity> entities, TransactionContext? transactionContext = null, ExecuteOptions? executeOptions = null)
            where TEntity : class;

        Task<bool> DeleteOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null);

        Task<int> DeleteManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null);

        /// <summary>
        /// 通过过滤器获取实体
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回获取到的实体实例，也有可能为 null</returns>
        Task<TEntity?> GetOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 通过过滤器获取实体，但只返回指定的字段
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="outFieldsFilter">需要返回的字段</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回获取到的实体实例，也有可能为 null</returns>
        Task<TEntity?> GetOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity>> outFieldsFilter,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 更新满足条件的第一个文档，但只更新指定的字段
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="updateDefinitions">待更新的字段</param>
        /// <param name="updateOptions">更新选项</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>更新成功则返回true；否则，返回false</returns>
        Task<bool> UpdateOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> updateDefinitions,
            UpdateOptions? updateOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 用于判断是否存在满足指定条件的文档
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="condition">条件</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>存在则返回true；否则，返回false</returns>
        Task<bool> ExistAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> condition,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 通过过滤器获取实体
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="sort">排序</param>
        /// <param name="options">配置项</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回获取到的实体实例，也有可能为 null</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="options"/> 的Skip小于0或Limit小于1时抛出此异常</exception>
        Task<IEnumerable<TEntity>> GetManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<SortDefinitionBuilder<TEntity>, SortDefinition<TEntity>>? sort = null,
            GetManyOptions? options = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 通过过滤器获取实体，但只返回指定的字段
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="outFieldsFilter">需要返回的字段</param>
        /// <param name="sort">排序</param>
        /// <param name="options">配置项</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回获取到的实体实例，也有可能为 null</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="options"/> 的Skip小于0或Limit小于1时抛出此异常</exception>
        Task<IEnumerable<TEntity>> GetManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity>> outFieldsFilter,
            Func<SortDefinitionBuilder<TEntity>, SortDefinition<TEntity>>? sort = null,
            GetManyOptions? options = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 更新满足条件的所有文档
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">更新条件</param>
        /// <param name="updateDefinitions">更新内容</param>
        /// <param name="updateManyOptions">更新选项</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回被更新的实体的数量</returns>
        Task<long> UpdateManyAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> updateDefinitions,
            UpdateManyOptions? updateManyOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 查找指定的文档，并更新该文档
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="updateDefinitions">更新内容</param>
        /// <param name="updateOptions">更新选项</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回指定的实体</returns>
        Task<TEntity?> FindOneAndUpdateAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> updateDefinitions,
            UpdateOptions? updateOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;

        /// <summary>
        /// 查找并替换指定的文档
        /// </summary>
        /// <typeparam name="TEntity">仓库实体类型</typeparam>
        /// <param name="filter">过滤器</param>
        /// <param name="doc">新文档</param>
        /// <param name="updateOptions">更新选项</param>
        /// <param name="transactionContext">事务上下文</param>
        /// <param name="executeOptions">数据库操作的执行选项</param>
        /// <returns>返回指定的实体</returns>
        Task<TEntity?> FindAndReplaceOneAsync<TEntity>(
            Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>> filter,
            TEntity doc,
            UpdateOptions? updateOptions = null,
            TransactionContext? transactionContext = null,
            ExecuteOptions? executeOptions = null)
            where TEntity : class;
    }
}