using ByteFlow.Asyncs;
using ByteFlow.Protocol;
using ByteFlow.WebSockets;
using ByteFlow.WebSockets.Events;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlow.Connection
{
    public class ByteProtoConnection : WebSockets.Connection
    {
        public event AsyncEventHandler? Opened;

        public event AsyncEventHandler<ConnectionClosedEventArgs>? Closed;

        public event AsyncEventHandler<ConnectionErrorEventArgs>? Error;

        public event AsyncEventHandler<PacketReceivedEventArgs>? PacketReceived;

        /// <summary>
        /// 发送心跳包的时间间隔，设置为 <see cref="TimeSpan.Zero"/> 时，不自动发送心跳包
        /// 默认值为 10s
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 连接是否有效，当当前时间距离上一个心跳包的时间小于两个心跳包发送间隔时间时为有效。
        /// </summary>
        public bool IsStillAlive => (DateTimeOffset.Now - _lastHeartbeatTime) < 2 * HeartbeatInterval && State == ConnectionState.Open;

        /// <summary>
        /// 收到上一个业务包的时间
        /// </summary>
        public DateTimeOffset PreviousPacketTime { get; private set; } = DateTimeOffset.Now;

        private readonly ScheduleTaskManager _taskManager = new();
        private DateTimeOffset _lastHeartbeatTime = DateTimeOffset.Now; // 记录上一次收到心跳包（响应）的时间

        public Task SendPacketAsync<TPacket>(TPacket packet, CancellationToken cancellationToken = default)
            where TPacket : class
        {
            var bytes = ByteProto.Packet(packet);
            return this.SendAsync(bytes, cancellationToken);
        }

        protected override async Task OnOpenedAsync()
        {
            if (this.HeartbeatInterval > TimeSpan.Zero)
            {
                _taskManager.ScheduleTask("__heartbeat", _ => this.SendAsync(ByteProto.HeartbeatRequestPacket), this.HeartbeatInterval);
                _lastHeartbeatTime = DateTimeOffset.Now;
            }

            if (this.Opened != null)
            {
                await this.Opened(this);
            }
        }

        protected override Task OnEventClosedAsync(ConnectionClosedEventArgs args)
        {
            if (this.Closed != null)
            {
                return this.Closed(this, args);
            }

            return base.OnEventClosedAsync(args);
        }

        protected override Task OnEventErrorAsync(ConnectionErrorEventArgs args)
        {
            if (this.Error != null)
            {
                return this.Error(this, args);
            }

            return base.OnEventErrorAsync(args);
        }

        protected override async Task OnEventMessageAsync(ConnectionMessage msg)
        {
            switch (msg.Type)
            {
                case ConnectionMessageType.Binary when this.PacketReceived == null:
                    return;
                case ConnectionMessageType.Binary:
                    {
                        var bin = msg.Binary ?? Array.Empty<byte>();
#if DEBUG
                        string bytesStr = string.Join(',', bin);
                        Console.WriteLine($"received binary, length:{bin.Length} bytes:[{bytesStr}]");
#endif
                        if (ByteProto.IsHeartbeatRequestPacket(bin))
                        {
                            // 服务端收到心跳包
                            this._lastHeartbeatTime = DateTimeOffset.Now;
                            // 发送心跳包的响应
                            await this.SendAsync(ByteProto.HeartbeatResponsePacket);
                        }
                        else if (ByteProto.IsHeartbeatResponsePacket(bin))
                        {
                            // 客户端收到心跳包响应
                            this._lastHeartbeatTime = DateTimeOffset.Now;
                        }
                        else
                        {
                            this.PreviousPacketTime = DateTimeOffset.Now;
                            try
                            {
                                var obj = ByteProto.Unpacket(bin);
                                await this.PacketReceived(this, new PacketReceivedEventArgs(obj));
                            }
                            catch (Exception e)
                            {
                                if (this.Error != null)
                                {
                                    await this.Error(this, new ConnectionErrorEventArgs("Error Occured while unpacking binary message", e));
                                }
                            }
                        }
                        return;
                    }
                case ConnectionMessageType.Text:
                    if (this.Error != null)
                    {
                        await this.Error(this, new ConnectionErrorEventArgs("Invalid Message", new WebSocketException(WebSocketError.InvalidMessageType)));
                    }
                    break;
            }
        }
    }
}
