﻿using ByteFlow.WebSockets.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.WebSockets
{
    public abstract class Connection : IDisposable
    {
        public string Tag { get; init; } = string.Empty;
        
        /// <summary>
        /// 可用于保存运行过程中与此连接相关的数据，而不需要保存额外的（连接->数据）的映射关系
        /// </summary>
        public object? UserData { get; set; }

        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        public string? SubProtocol => this.InternalWebSocket?.SubProtocol;

        public ConnectionState State => this.InternalWebSocket == null ? ConnectionState.None : this.InternalWebSocket.State switch
        {
            WebSocketState.Connecting => ConnectionState.Connecting,
            WebSocketState.Open => ConnectionState.Open,
            WebSocketState.CloseSent or WebSocketState.CloseReceived or WebSocketState.Closed => ConnectionState.Closed,
            _ => ConnectionState.None,
        };

        public bool IsClientConnection => this.InternalWebSocket is ClientWebSocket;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        internal WebSocket? InternalWebSocket { get; set; }
        private ConnectionMessageReceiver? _receiver;
        private bool _disposed;
        private bool _closed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Connection()
        {
            Dispose(false);
        }

        //这里的参数表示示是否需要释放那些实现IDisposable接口的托管对象
        protected virtual async void Dispose(bool disposing)
        {
            if (_disposed) return; //如果已经被回收，就中断执行
            if (disposing)
            {
                // 释放那些实现IDisposable接口的托管对象
                await CloseInternalWebSocketAsync(WebSocketCloseStatus.NormalClosure, "Normal Close", CancellationToken.None);
            }

            // 释放非托管资源，设置对象为null
            await this.CloseAsync();

            _disposed = true;
        }

        /// <summary>
        /// 连接到指定的地址【客户端使用】
        /// </summary>
        /// <param name="urls">待连接的地址</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="ignoreUrlTest">是否忽略Url连接性测试，如果为true，则会选择指定的urls地址的第一个进行连接</param>
        public async Task ConnectAsync(IEnumerable<string> urls, CancellationToken cancellationToken, bool ignoreUrlTest = false)
        {
            if (!IsClientConnection)
            {
                throw new InvalidOperationException($"Cannot invoke {nameof(ConnectAsync)} on server connection.");
            }
            if (!urls.Any())
            {
                throw new ArgumentException("为 WebSocket 提供的 url 地址不能为空");
            }

            var url = urls.FirstOrDefault() ?? string.Empty;
            if (!ignoreUrlTest)
            {
                // 如果不忽略 Url 连接性测试，则需要测试可连接性
                url = await ConnectionUtils.UrlsConnectiveTest(urls, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("未找到合适的连接地址");
            }

            var clientWs = (this.InternalWebSocket as ClientWebSocket)!;
            await clientWs.ConnectAsync(new Uri(url), cancellationToken);

            await this.StartReceiverAsync();
            await this.OnOpenedAsync();
        }

        /// <summary>
        /// 启动服务端WebSocket，开始监听客户端的连接【服务端使用】
        /// </summary>
        public Task StartServeAsync()
        {
            if (this.IsClientConnection)
            {
                throw new InvalidOperationException($"Cannot invoke {nameof(StartServeAsync)} on client connection.");
            }
            return this.StartReceiverAsync();
        }

        public Task CloseAsync() => this.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Close", CancellationToken.None);

        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string? desc, CancellationToken cancellationToken)
        {
            if (_closed)
            {
                return;
            }
            _closed = true;
            // 停止接收消息
            this.StopReceiver();
            // 关闭底层 WebSocket
            await this.CloseInternalWebSocketAsync(closeStatus, desc, cancellationToken);
            // 执行其他关闭逻辑
            await this.OnClosingAsync();
        }

        public Task SendAsync(ArraySegment<byte> arr, CancellationToken cancellationToken = default)
        {
            if (this.State != ConnectionState.Open)
            {
                return Task.CompletedTask;
            }
#if DEBUG
            string bytesStr = string.Join(',', arr.Array ?? Array.Empty<byte>());
            Console.WriteLine($"sending binary, length:{arr.Count} bytes:[{bytesStr}]");
#endif
            return this.InternalWebSocket != null ? this.InternalWebSocket.SendAsync(arr, WebSocketMessageType.Binary, true, cancellationToken) : Task.CompletedTask;
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> arr, CancellationToken cancellationToken = default)
        {
            if (this.State != ConnectionState.Open)
            {
                return ValueTask.CompletedTask;
            }

#if DEBUG
            string bytesStr = string.Join(',', arr.ToArray());
            Console.WriteLine($"sending binary, length:{arr.Length} bytes:[{bytesStr}]");
#endif
            return InternalWebSocket?.SendAsync(arr, WebSocketMessageType.Binary, true, cancellationToken) ?? ValueTask.CompletedTask;
        }

        public Task SendAsync(string text, CancellationToken cancellationToken = default)
        {
            if (this.State != ConnectionState.Open)
            {
                return Task.CompletedTask;
            }

#if DEBUG
            Console.WriteLine($"sending text, length:{text.Length} content:{text}");
#endif
            var txtBytes = this.TextEncoding.GetBytes(text);
            return this.InternalWebSocket != null ? this.InternalWebSocket.SendAsync(new ArraySegment<byte>(txtBytes), WebSocketMessageType.Text, true, cancellationToken) : Task.CompletedTask;
        }

        /// <summary>
        /// 成功启动WebSocket，且开始正常接受数据时触发【仅客户端有效】
        /// </summary>
        protected virtual Task OnOpenedAsync() => Task.CompletedTask;

        /// <summary>
        /// 准备关闭当前连接时触发
        /// </summary>
        /// <returns></returns>
        protected virtual Task OnClosingAsync() => Task.CompletedTask;

        protected virtual Task OnEventClosedAsync(ConnectionClosedEventArgs args) => Task.CompletedTask;

        protected virtual Task OnEventErrorAsync(ConnectionErrorEventArgs args) => Task.CompletedTask;

        protected virtual Task OnEventMessageAsync(ConnectionMessage msg) => Task.CompletedTask;

        private async Task StartReceiverAsync()
        {
            this.StopReceiver();
            if (InternalWebSocket == null)
            {
                throw new InvalidOperationException($"The internal websocket seemly has disposed.");
            }

            this._receiver = new ConnectionMessageReceiver(InternalWebSocket, TextEncoding)
            {
                Tag = this.Tag,
                ErrorAsyncAction = OnEventErrorAsync,
                ClosedByRemoteAsyncAction = this.OnEventClosedAsync,
                MessageAsyncAction = OnEventMessageAsync,
            };

            // 开始接受数据
            await this._receiver.Start();
        }

        private void StopReceiver()
        {
            this._receiver?.Stop();
            this._receiver = null;
        }

        private async Task CloseInternalWebSocketAsync(WebSocketCloseStatus closeStatus, string? desc, CancellationToken cancellationToken)
        {
            if (InternalWebSocket == null)
            {
                return;
            }
            try
            {
                if (this.State == ConnectionState.Closed || this.InternalWebSocket.State == WebSocketState.Aborted)
                {
                    return;
                }
                await this.InternalWebSocket.CloseAsync(closeStatus, desc, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                InternalWebSocket?.Dispose();
                InternalWebSocket = null;
            }
        }
    }
}
