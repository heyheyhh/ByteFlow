using ByteFlow.Asyncs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ByteFlow.Core.Extensions
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// 遍历集合，并在每个元素上执行指定的操作
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="col">集合</param>
        /// <param name="action">待执行的操作</param>
        public static void ForEach<T>(this IEnumerable<T> col, Action<T> action)
        {
            foreach (var item in col)
            {
                action(item);
            }
        }

        /// <summary>
        /// 遍历集合，当元素满足 <paramref name="ifCondition"/> 时，在该元素上执行指定的操作
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="col">集合</param>
        /// <param name="ifCondition">条件</param>
        /// <param name="action">待执行的操作</param>
        public static void ForEachIf<T>(this IEnumerable<T> col, Func<T, bool> ifCondition, Action<T> action)
        {
            foreach (var item in col)
            {
                if (ifCondition(item))
                {
                    action(item);
                }
            }
        }

        /// <summary>
        /// 遍历集合，并在每个元素上执行指定的操作
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="col">集合</param>
        /// <param name="action">待执行的操作</param>
        public static async Task ForEach<T>(this IAsyncEnumerable<T> col, AsyncAction<T> action)
        {
            await foreach (var item in col)
            {
                await action(item);
            }
        }

        /// <summary>
        /// 遍历集合，当元素满足 <paramref name="ifCondition"/> 时，在该元素上执行指定的操作
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="col">集合</param>
        /// <param name="ifCondition">条件</param>
        /// <param name="action">待执行的操作</param>
        public static async Task ForEachIf<T>(this IAsyncEnumerable<T> col, Func<T, bool> ifCondition, AsyncAction<T> action)
        {
            await foreach (var item in col)
            {
                if (ifCondition(item))
                {
                    await action(item);
                }
            }
        }
    }
}
