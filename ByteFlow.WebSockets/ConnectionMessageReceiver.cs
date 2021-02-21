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

        public bool IsClientWebSocket => _socket is ClientWebSocket;

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
            _socket = socket;
            TextEncoding = textEncoding ?? Encoding.UTF8;
        }

        public async Task Start()
        {
            StartConsumer();
            await StartReceiver();
        }

        public void Stop()
        {
            StopReceiver();
            StopConsumer();
        }

        private async Task StartReceiver()
        {
            StopReceiver();
            _msgReceiveTokenSource = new CancellationTokenSource();

            if (IsClientWebSocket)
            {
                // 在客户端，不能阻塞当前线程，否则无法进行其他业务逻辑的处理
                await Task.Factory.StartNew(OnReceiveCore, _msgReceiveTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            else
            {
                // 在服务器端，需要阻塞当前线程，否则连接会立即断开
                await OnReceiveCore();
            }
        }

        private async Task OnReceiveCore()
        {
            if (_msgReceiveTokenSource == null)
            {
                return;
            }
            try
            {
                var token = _msgReceiveTokenSource.Token;
                var socket = _socket;

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
                if (ErrorAsyncAction != null)
                {
                    await ErrorAsyncAction(args);
                }
            }
        }

        private void StartConsumer()
        {
            StopConsumer();
            _msgConsumeTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (await _messagesChannel.Reader.WaitToReadAsync())
                {
                    if (_msgConsumeTokenSource == null || _msgConsumeTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!_messagesChannel.Reader.TryRead(out var msg)) continue;
                    if (MessageAsyncAction != null)
                    {
                        await MessageAsyncAction(msg);
                    }
                }
            }, _msgConsumeTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void StopReceiver()
        {
            if (_msgReceiveTokenSource is null)
            {
                return;
            }
            try
            {
                using (_msgReceiveTokenSource)
                {
                    _msgReceiveTokenSource.Cancel();
                }

                _messagesChannel.Writer.TryComplete();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                _msgReceiveTokenSource = null;
            }
        }

        private void StopConsumer()
        {
            if (_msgConsumeTokenSource is null)
            {
                return;
            }
            try
            {
                using (_msgConsumeTokenSource)
                {
                    _msgConsumeTokenSource.Cancel();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                _msgConsumeTokenSource = null;
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
                        if (ClosedByRemoteAsyncAction is null) return null;
                        
                        var closeStatus = recv.CloseStatus ?? WebSocketCloseStatus.Empty;
                        var closeDesc = recv.CloseStatusDescription ?? "close by remote";
                        await ClosedByRemoteAsyncAction(new ConnectionClosedEventArgs(closeStatus, closeDesc, true));
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
                WebSocketMessageType.Text => new ConnectionMessage(ConnectionMessageType.Text, TextEncoding.GetString(msgStream.ToArray())),
                WebSocketMessageType.Binary => new ConnectionMessage(ConnectionMessageType.Binary, string.Empty, msgStream.ToArray()),
                _ => null,
            };
        }
    }
}
