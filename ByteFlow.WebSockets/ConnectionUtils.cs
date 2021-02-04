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
        public static async Task<string?> UrlsConnectiveTest(IEnumerable<string> urls, CancellationToken cancellationToken)
        {
            var listUrls = new List<string>(urls);
            foreach (var url in listUrls)
            {
                try
                {
                    using var ws = new ClientWebSocket();
                    var tmpTask = ws.ConnectAsync(new Uri(url), cancellationToken);
                    var delay = Task.Delay(2000, cancellationToken);
                    var compTask = await Task.WhenAny(tmpTask, delay);
                    if (compTask == delay || compTask.Id == delay.Id)
                    {
                        continue; // 超时
                    }
                    //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close Normally", cancellationToken);
                    return url;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);

                    // 100ms 之后继续尝试
                    await Task.Delay(500, cancellationToken);
                }
            }
            return null;
        }
    }
}
