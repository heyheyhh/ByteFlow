using ByteFlow.WebSockets.Events;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ByteFlow.WebSockets
{
    internal delegate Task AsyncAction<in T>(T t);

    internal sealed class ConnectionMessageReceiver
    {
        public AsyncAction<ConnectionErrorEventArgs>? ErrorAsyncAction { get; set; }
        public AsyncAction<ConnectionClosedEventArgs>? ClosedByRemoteAsyncAction { get; set; }
        public AsyncAction<ConnectionMessage>? MessageAsyncAction { get; set; }
        public string Tag { get; set; } = string.Empty;

        public Encoding TextEncoding { get; }

        public bool IsClientWebSocket => this._socket is ClientWebSocket;

        private readonly Channel<ConnectionMessage> _messagesChannel = Channel.CreateUnbounded<ConnectionMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        private readonly WebSocket _socket;
        private CancellationTokenSource? _msgReceiveTokenSource;
        private CancellationTokenSource? _msgConsumeTokenSource;

        internal ConnectionMessageReceiver(WebSocket socket, Encoding? textEncoding = null)
        {
            this._socket = socket;
            this.TextEncoding = textEncoding ?? Encoding.UTF8;
        }

        public async Task Start()
        {
            this.StartConsumer();
            await this.StartReceiver();
        }

        public void Stop()
        {
            this.StopReceiver();
            this.StopConsumer();
        }

        private async Task StartReceiver()
        {
            this.StopReceiver();
            this._msgReceiveTokenSource = new CancellationTokenSource();

            if (this.IsClientWebSocket)
            {
                // 在客户端，不能阻塞当前线程，否则无法进行其他业务逻辑的处理
                await Task.Factory.StartNew(OnReceiveCore, this._msgReceiveTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            else
            {
                // 在服务器端，需要阻塞当前线程，否则连接会立即断开
                await OnReceiveCore();
            }
        }

        private async Task OnReceiveCore()
        {
            if (this._msgReceiveTokenSource == null)
            {
                return;
            }
            try
            {
                var token = this._msgReceiveTokenSource.Token;
                var socket = this._socket;

                while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    // 收取一个完整的包
                    var msg = await ReadPacket(socket, token);
                    if (msg is null)
                    {
                        continue;
                    }

                    await _messagesChannel.Writer.WriteAsync(msg, token);
                }
            }
            catch (Exception e)
            {
                var args = new ConnectionErrorEventArgs(e.Message, e);
                Debug.WriteLine($"Tag:{Tag}, Exp:{e.Message}");
                if (this.ErrorAsyncAction != null)
                {
                    await this.ErrorAsyncAction(args);
                }
            }
        }

        private void StartConsumer()
        {
            this.StopConsumer();
            this._msgConsumeTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (await this._messagesChannel.Reader.WaitToReadAsync())
                {
                    if (this._msgConsumeTokenSource == null || this._msgConsumeTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!this._messagesChannel.Reader.TryRead(out var msg)) continue;
                    if (this.MessageAsyncAction != null)
                    {
                        await this.MessageAsyncAction(msg);
                    }
                }
            }, this._msgConsumeTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void StopReceiver()
        {
            if (this._msgReceiveTokenSource is null)
            {
                return;
            }
            try
            {
                using (this._msgReceiveTokenSource)
                {
                    this._msgReceiveTokenSource.Cancel();
                }

                _messagesChannel.Writer.TryComplete();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                this._msgReceiveTokenSource = null;
            }
        }

        private void StopConsumer()
        {
            if (this._msgConsumeTokenSource is null)
            {
                return;
            }
            try
            {
                using (this._msgConsumeTokenSource)
                {
                    this._msgConsumeTokenSource.Cancel();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                this._msgConsumeTokenSource = null;
            }
        }

        private async Task<ConnectionMessage?> ReadPacket(WebSocket socket, CancellationToken token)
        {
            WebSocketMessageType? msgType = null;
            await using var msgStream = new MemoryStream();
            while (true)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    var recv = await socket.ReceiveAsync(buffer, token);
                    if (recv.MessageType == WebSocketMessageType.Close)
                    {
                        if (this.ClosedByRemoteAsyncAction is null) return null;
                        
                        var closeStatus = recv.CloseStatus ?? WebSocketCloseStatus.Empty;
                        var closeDesc = recv.CloseStatusDescription ?? "close by remote";
                        await this.ClosedByRemoteAsyncAction(new ConnectionClosedEventArgs(closeStatus, closeDesc, true));
                        return null;
                    }

                    msgType = recv.MessageType;
                    await msgStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, recv.Count), token);

                    if (recv.EndOfMessage)
                    {
                        break; // 已经接收完一条完整的消息
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, true);
                }
            }

            await msgStream.FlushAsync(token);

            return msgType switch
            {
                WebSocketMessageType.Text => new ConnectionMessage(ConnectionMessageType.Text, this.TextEncoding.GetString(msgStream.ToArray())),
                WebSocketMessageType.Binary => new ConnectionMessage(ConnectionMessageType.Binary, string.Empty, msgStream.ToArray()),
                _ => null,
            };
        }
    }
}
