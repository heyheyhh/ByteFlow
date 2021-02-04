using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    public delegate Task<TResult> AsyncFunc<TResult>();
    public delegate Task<TResult> AsyncFunc<in T, TResult>(T t);
    public delegate Task<TResult> AsyncFunc<in T1, in T2, TResult>(T1 t1, T2 t2);
    public delegate Task<TResult> AsyncFunc<in T1, in T2, in T3, TResult>(T1 t1, T2 t2, T3 t3);
}
