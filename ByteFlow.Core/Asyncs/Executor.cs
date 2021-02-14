using ByteFlow.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    public static class Executor
    {
        /// <summary>
        /// 运行可超时的任务
        /// </summary>
        /// <param name="action">需要执行的操作</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>如果成功执行了任务，返回True；否则（如超时），返回False</returns>
        public static async Task<bool> RunAsync(Action action, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var mainTask = Task.Run(action, tokenSource.Token);
            var timeoutTask = Task.Delay(timeout, tokenSource.Token);
            Task firstCompleteTask = await Task.WhenAny(mainTask, timeoutTask);
            tokenSource.Cancel();
            return firstCompleteTask != timeoutTask && firstCompleteTask.Id != timeoutTask.Id;
        }
        /// <summary>
        /// 运行可超时的任务
        /// </summary>
        /// <param name="action">需要执行的操作</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>如果成功执行了任务，返回True；否则（如超时），返回False</returns>
        public static async Task<bool> RunAsync(AsyncAction<CancellationToken> action, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var mainTask = action(tokenSource.Token);
            var timeoutTask = Task.Delay(timeout, tokenSource.Token);
            Task firstCompleteTask = await Task.WhenAny(mainTask, timeoutTask);
            tokenSource.Cancel();
            return firstCompleteTask != timeoutTask && firstCompleteTask.Id != timeoutTask.Id;
        }

        /// <summary>
        /// 运行可超时的任务
        /// </summary>
        /// <typeparam name="TResult">返回的结果的类型</typeparam>
        /// <param name="func">需要执行的操作</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>如果成功执行了任务，返回结果；否则（如超时），返回default</returns>
        public static async Task<TResult> RunAsync<TResult>(Func<TResult> func, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var mainTask = Task.Run(func, tokenSource.Token);
            var timeoutTask = Task.Delay(timeout, tokenSource.Token);
            Task firstCompleteTask = await Task.WhenAny(mainTask, timeoutTask);
            tokenSource.Cancel();
            if (firstCompleteTask == timeoutTask || firstCompleteTask.Id == timeoutTask.Id)
            {
                // timeout
#pragma warning disable CS8603 // 可能的 null 引用返回。
                return default;
#pragma warning restore CS8603 // 可能的 null 引用返回。
            }

            return mainTask.Result;
        }
        /// <summary>
        /// 运行可超时的任务
        /// </summary>
        /// <typeparam name="TResult">返回的结果的类型</typeparam>
        /// <param name="func">需要执行的操作</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>如果成功执行了任务，返回结果；否则（如超时），返回default</returns>
        public static async Task<TResult> RunAsync<TResult>(AsyncFunc<CancellationToken, TResult> func, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var mainTask = func(tokenSource.Token);
            var timeoutTask = Task.Delay(timeout, tokenSource.Token);
            Task firstCompleteTask = await Task.WhenAny(mainTask, timeoutTask);
            tokenSource.Cancel();
            if (firstCompleteTask == timeoutTask || firstCompleteTask.Id == timeoutTask.Id)
            {
                // timeout
#pragma warning disable CS8603 // 可能的 null 引用返回。
                return default;
#pragma warning restore CS8603 // 可能的 null 引用返回。
            }

            return mainTask.Result;
        }

        /// <summary>
        /// 在指定的时间之后运行任务
        /// </summary>
        /// <param name="delay">需要延迟的时间</param>
        /// <param name="action">需要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Task</returns>
        public static Task RunAfterAsync(TimeSpan delay, Action action, CancellationToken cancellationToken = default)
            => Task.Delay(delay, cancellationToken).ContinueWith(_ => action(), cancellationToken);
        /// <summary>
        /// 在指定的时间之后运行任务
        /// </summary>
        /// <param name="delay">需要延迟的时间</param>
        /// <param name="action">需要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Task</returns>
        public static async Task RunAfterAsync(TimeSpan delay, AsyncAction<CancellationToken> action, CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            await action(cancellationToken);
        }

        /// <summary>
        /// 在指定的时间之后运行任务
        /// </summary>
        /// <typeparam name="TResult">返回结果的类型</typeparam>
        /// <param name="delay">需要延迟的时间</param>
        /// <param name="action">需要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>结果</returns>
        public static Task<TResult> RunAfterAsync<TResult>(TimeSpan delay, Func<TResult> action, CancellationToken cancellationToken = default)
            => Task.Delay(delay, cancellationToken).ContinueWith(_ => action(), cancellationToken);
        /// <summary>
        /// 在指定的时间之后运行任务
        /// </summary>
        /// <typeparam name="TResult">返回结果的类型</typeparam>
        /// <param name="delay">需要延迟的时间</param>
        /// <param name="action">需要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>结果</returns>
        public static async Task<TResult> RunAfterAsync<TResult>(TimeSpan delay, AsyncFunc<CancellationToken, TResult> action, CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            return await action(cancellationToken);
        }

        /// <summary>
        /// 启动一个需要长时间运行的任务
        /// </summary>
        /// <param name="action">待执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Task</returns>
        public static Task RunLongTimeAsync(Action action, CancellationToken cancellationToken)
            => Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        /// <summary>
        /// 启动一个需要长时间运行的任务
        /// </summary>
        /// <typeparam name="TResult">返回结果的类型</typeparam>
        /// <param name="func">需要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>结果</returns>
        public static Task<TResult> RunLongTimeAsync<TResult>(Func<TResult> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        /// <summary>
        /// 等待某个条件满足再继续执行
        /// </summary>
        /// <remarks>
        /// 注意：
        /// 此方法在周期性的调用 <paramref name="checkFunc"/> 时，不会处理 <paramref name="checkFunc"/> 可能抛出的异常
        /// </remarks>
        /// <param name="checkFunc">用于检查指定条件是否满足</param>
        /// <param name="checkDuration">执行<paramref name="checkFunc"/>的时间间隔</param>
        /// <param name="timeout">当前等待的超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <exception cref="TimeoutException">当等待时间超过指定的<paramref name="timeout"/>时间时</exception>
        public static async Task WaitUntilAsync(AsyncFunc<bool> checkFunc, TimeSpan checkDuration, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            var workTask = RunLongTimeAsync(async () =>
            {
                var checkRes = false;
                while (!token.IsCancellationRequested && !checkRes)
                {
                    await Task.Delay(checkDuration, cancellationToken);
                    checkRes = await checkFunc();
                }
            }, token);
            var timeoutTask = Task.Delay(timeout, token);
            Task firstCompleteTask = await Task.WhenAny(workTask, timeoutTask);
            tokenSource.Cancel();
            if (firstCompleteTask == timeoutTask || firstCompleteTask.Id == timeoutTask.Id)
            {
                throw new TimeoutException($"Time exceed {timeout.ToHHMMSS()}");
            }
        }
    }
}
