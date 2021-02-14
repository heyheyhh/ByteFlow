using System;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    public class AsyncLock
    {
        private readonly SemaphoreSlim semaphore;

        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
        }

        public Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            var wait = semaphore.WaitAsync(cancellationToken);
            if (wait.IsCompleted)
            {
                return Task.FromResult((IDisposable)new LockReleaser(this));
            }
            
            return wait.ContinueWith(
                    _ => (IDisposable)new LockReleaser(this),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private class LockReleaser : IDisposable
        {
            private AsyncLock? target;

            internal LockReleaser(AsyncLock target)
            {
                this.target = target;
            }

            public void Dispose()
            {
                if (target == null)
                    return;

                // first null it, next Release, so even if Release throws, we don't hold the reference any more.
                AsyncLock tmp = target;
                target = null;
                try
                {
                    tmp.semaphore.Release();
                }
                catch (Exception) { } // just ignore the Exception
            }
        }
    }
}
