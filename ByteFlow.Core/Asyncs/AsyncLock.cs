﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    internal class AsyncSemaphore
    {
        private static readonly Task SCompleted = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> _mWaiters = new Queue<TaskCompletionSource<bool>>();
        private int _mCurrentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));
            _mCurrentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (_mWaiters)
            {
                if (_mCurrentCount > 0)
                {
                    --_mCurrentCount;
                    return SCompleted;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    _mWaiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool>? toRelease = null;
            lock (_mWaiters)
            {
                if (_mWaiters.Count > 0)
                    toRelease = _mWaiters.Dequeue();
                else
                    ++_mCurrentCount;
            }

            toRelease?.SetResult(true);
        }
    }

    public class AsyncLock
    {
        private readonly AsyncSemaphore _mSemaphore;
        private readonly Task<Releaser> _mReleaser;

        public AsyncLock()
        {
            _mSemaphore = new AsyncSemaphore(1);
            _mReleaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            var wait = _mSemaphore.WaitAsync();
            return wait.IsCompleted ? 
                _mReleaser :
                wait.ContinueWith((_, state) => new Releaser((state as AsyncLock)!),this, CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public readonly struct Releaser : IDisposable
        {
            private readonly AsyncLock _mToRelease;

            internal Releaser(AsyncLock toRelease) { _mToRelease = toRelease; }

            public void Dispose()
            {
                _mToRelease._mSemaphore.Release();
            }
        }
    }
}
