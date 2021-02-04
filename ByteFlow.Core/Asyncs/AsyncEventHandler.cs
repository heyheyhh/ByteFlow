using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    /// <summary>
    /// 异步的事件处理程序
    /// </summary>
    public delegate Task AsyncEventHandler(object? sender);

    /// <summary>
    /// 带参数的异步的事件处理程序
    /// </summary>
    /// <typeparam name="TArgs">参数的类型</typeparam>
    /// <param name="args">参数</param>
    public delegate Task AsyncEventHandler<in TArgs>(object? sender, TArgs args);

    public delegate Task<TResult> AsyncFuncEventHandler<TResult>(object? sender);

    public delegate Task<TResult> AsyncFuncEventHandler<TResult, in TArgs>(object? sender, TArgs args);
}
