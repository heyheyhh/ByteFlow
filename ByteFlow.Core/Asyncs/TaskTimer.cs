using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    /// <summary>
    /// 由 Task 模拟的定时器，不一定精确。在对定时器精度要求不高时，可以使用
    /// </summary>
    public class TaskTimer
    {
        public event Action<TimeSpan>? Tick;

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
            if (this._interval.TotalMilliseconds < 10)
            {
                throw new NotSupportedException("此Timer不支持小于10ms的Tick周期");
            }
            this.RunInternal();
        }

        /// <summary>
        /// 停止Timer的运行
        /// </summary>
        public void Stop()
        {
            _startTime = DateTimeOffset.Now;
            if (this._tokenSource == null)
            {
                return;
            }

            try
            {
                this._tokenSource.Cancel(false);
                this._tokenSource.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                this._tokenSource = null;
            }
        }

        private void RunInternal()
        {
            this.Stop();
            this._tokenSource = new CancellationTokenSource();

            Executor.RunLongTimeAsync(async () =>
            {
                _startTime = DateTimeOffset.Now;
                while (CanKeepRun())
                {
                    await Task.Delay(this._interval);
                    if (!CanKeepRun())
                    {
                        break;
                    }

                    try
                    {
                        var duration = DateTimeOffset.Now - _startTime;
                        this.Tick?.Invoke(duration);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }, this._tokenSource.Token);

            bool CanKeepRun() => this._tokenSource != null && !this._tokenSource.IsCancellationRequested;
        }
    }
}
