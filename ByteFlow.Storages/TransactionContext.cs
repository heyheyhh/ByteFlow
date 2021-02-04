using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ByteFlow.Storages
{
    public sealed class TransactionContext
    {
        internal IClientSessionHandle Session { get; private set; }

        internal TransactionContext(IClientSessionHandle sessionHandle)
        {
            this.Session = sessionHandle ?? throw new ArgumentNullException(nameof(sessionHandle));
        }

        public Task AbortTransactionAsync(CancellationToken cancellationToken = default)
            => this.Session.AbortTransactionAsync(cancellationToken);
    }
}