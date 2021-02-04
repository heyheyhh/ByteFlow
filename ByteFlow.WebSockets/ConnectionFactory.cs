using System;
using System.Net.WebSockets;

namespace ByteFlow.WebSockets
{
    public class ConnectionFactory
    {
        /// <summary>
        /// 创建一个客户端连接
        /// </summary>
        /// <param name="tag">为该连接指定的可辨识的标志</param>
        /// <param name="config">配置连接，如子协议，请求头等</param>
        /// <param name="keepAliveMilliseconds">发送KeepAlive的间隔时间（单位：毫秒），为 0 则表示禁用KeepAlive。默认为0</param>
        public static TConnection CreateClient<TConnection>(string? tag = null, Action<ClientWebSocketOptions>? config = null, uint keepAliveMilliseconds = 0)
            where TConnection : Connection, new()
        {
            var ws = new ClientWebSocket();
            // TimeSpan.Zero 时禁用KeepAlive
            ws.Options.KeepAliveInterval = keepAliveMilliseconds == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(keepAliveMilliseconds);
            config?.Invoke(ws.Options);
            return new TConnection()
            {
                Tag = tag ?? string.Empty,
                InternalWebSocket = ws,
            };
        }

        /// <summary>
        /// 创建一个服务端连接
        /// </summary>
        /// <param name="ws">服务端WebSocket</param>
        /// <param name="tag">为该连接指定的可辨识的标志</param>
        public static TConnection CreateServer<TConnection>(WebSocket ws, string? tag = null)
            where TConnection : Connection, new()
        {
            if (ws is ClientWebSocket)
            {
                throw new InvalidOperationException($"不能使用 {nameof(CreateServer)} 创建客户端连接，应该使用 ${nameof(CreateClient)}");
            }
            return new TConnection()
            {
                Tag = tag ?? string.Empty,
                InternalWebSocket = ws,
            };
        }
    }
}
