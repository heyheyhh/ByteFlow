using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Caches
{
    public class CacheProviderHealthCheck : IHealthCheck
    {
        private readonly ICacheProvider cacheProvider;

        public CacheProviderHealthCheck(ICacheProvider provider)
        {
            this.cacheProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return this.cacheProvider.CheckHealthAsync();
        }
    }
}
