using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ByteFlow.Storages.HealthChecks
{
    /// <summary>
    /// MongoDB 存储提供程序的健康检查
    /// </summary>
    public class StorageProviderHealthCheck : IHealthCheck
    {
        private readonly IStorageProvider _storageProvider;

        public StorageProviderHealthCheck(IStorageProvider storageProvider)
        {
            this._storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => this._storageProvider.CheckHealthAsync();
    }
}