using System.Threading.Tasks;

namespace ByteFlow.Asyncs
{
    public delegate Task AsyncAction();
    public delegate Task AsyncAction<in T>(T t);
    public delegate Task AsyncAction<in T1, in T2>(T1 t1, T2 t2);
    public delegate Task AsyncAction<in T1, in T2, in T3>(T1 t1, T2 t2, T3 t3);
}
