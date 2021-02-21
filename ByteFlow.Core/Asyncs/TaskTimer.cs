using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ByteFlow.Extensions;
using ByteFlow.Helpers;

namespace ByteFlow.Asyncs
{
    /// <summary>
    /// 由 Task 模拟的定时器，不一定精确。在对定时器精度要求不高时，可以使用
    /// </summary>
    public class TaskTimer
    {
        public event Action<TimeSpan>? Tick;

        public event AsyncAction<TimeSpan, CancellationToken>? TickAsync;

        private DateTimeOffset _startTime;
        private CancellationTokenSource? _tokenSource;
        private TimeSpan _interval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 开始运行Timer
        /// </summary>
        /// <param name="interval">Timer Tick的周期，如果不指定，则默认为 1s</param>
        public void Start(TimeSpan? interval = null)
        {
            _startTime = DateTimeOffset.Now;
            _interval = interval ?? TimeSpan.FromSeconds(1);
            if (_interval.TotalMilliseconds < 10)
            {
                throw new NotSupportedException("此Timer不支持小于10ms的Tick周期");
            }
            RunInternal();
        }

        /// <summary>
        /// 停止Timer的运行
        /// </summary>
        public void Stop()
        {
            _startTime = DateTimeOffset.Now;
            TokenSourceHelper.Dispose(ref _tokenSource);
        }

        private void RunInternal()
        {
            Stop();
            
            TokenSourceHelper.Create(ref _tokenSource, out var token);

            Executor.RunLongTimeAsync(async () =>
            {
                _startTime = DateTimeOffset.Now;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_interval, token);
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        var duration = DateTimeOffset.Now - _startTime;
                        if (TickAsync != null)
                        {
                            TickAsync(duration, token).Ignore();
                        }
                        else
                        {
                            Tick?.Invoke(duration);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }, token);
        }
    }
}
