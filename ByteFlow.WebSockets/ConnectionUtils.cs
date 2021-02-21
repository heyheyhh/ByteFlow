using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.WebSockets
{
    public class ConnectionUtils
    {
        /// <summary>
        /// 测试指定Url地址的可连接性。
        /// </summary>
        /// <param name="urls">待测试的Url集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>返回第一个可以正常连接的Url，如果没有找到可以连接的地址，则返回 null</returns>
        [Obsolete("请使用 UrlsConnectiveTestAsync ")]
        public static Task<string?> UrlsConnectiveTest(IEnumerable<string> urls, CancellationToken cancellationToken)
            => UrlsConnectiveTestAsync(urls, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(500), cancellationToken);

        /// <summary>
        /// 测试指定Url地址的可连接性。
        /// </summary>
        /// <param name="urls">待测试的Url集合</param>
        /// <param name="timeoutPerTest">每一个测试的超时时间</param>
        /// <param name="durationForEachTest">各个测试之间的间隔时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>返回第一个可以正常连接的Url，如果没有找到可以连接的地址，则返回 null</returns>
        public static async Task<string?> UrlsConnectiveTestAsync(IEnumerable<string> urls, TimeSpan timeoutPerTest, TimeSpan durationForEachTest, CancellationToken cancellationToken)
        {
            var listUrls = new List<string>(urls);
            foreach (var url in listUrls)
            {
                try
                {
                    using var ws = new ClientWebSocket();
                    var tmpTask = ws.ConnectAsync(new Uri(url), cancellationToken);
                    var delay = Task.Delay(timeoutPerTest, cancellationToken);
                    var compTask = await Task.WhenAny(tmpTask, delay);
                    if (compTask == delay || compTask.Id == delay.Id)
                    {
                        continue; // 超时
                    }

                    return url;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);

                    // 100ms 之后继续尝试
                    await Task.Delay(durationForEachTest, cancellationToken);
                }
            }
            return null;
        }
    }
}
