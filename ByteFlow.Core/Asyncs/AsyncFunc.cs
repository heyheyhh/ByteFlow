using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    public delegate Task<TResult> AsyncFunc<TResult>(CancellationToken cancellationToken = default);
    public delegate Task<TResult> AsyncFunc<in T, TResult>(T t, CancellationToken cancellationToken = default);
    public delegate Task<TResult> AsyncFunc<in T1, in T2, TResult>(T1 t1, T2 t2, CancellationToken cancellationToken = default);
    public delegate Task<TResult> AsyncFunc<in T1, in T2, in T3, TResult>(T1 t1, T2 t2, T3 t3, CancellationToken cancellationToken = default);
}
