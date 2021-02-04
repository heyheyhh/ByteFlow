using ByteFlow.Asyncs;
using ByteFlow.Connection;
using ByteFlow.Extensions;
using ByteFlow.Protocol;
using ByteFlow.WebSockets;
using ByteFlow.WebSockets.Events;
using ByteFlowTestEntities;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ByteFlowTest
{
    [ByteProtoPacket(11)]
    public class SimpleEntity
    {
        [ByteProtoMember(1)]
        public int Id { get; set; }
        
        [ByteProtoMember(2)]
        public string Desc { get; set; } = string.Empty;
        
        [ByteProtoMember(3)]
        public DateTimeOffset Time { get; set; }
    }

    internal class Program
    {
        private static ByteProtoConnection? _client;

        private static async Task Main(string[] _)
        {
            ByteProto.RegisterTypes(typeof(Entity).Assembly, typeof(SimpleEntity).Assembly);
            // PackTest.Test();
            
            _client = ConnectionFactory.CreateClient<ByteProtoConnection>("TEST_CLIENT", options => {
                options.AddSubProtocol("byte_proto");
                options.SetRequestHeader("Authorization", "sid 7903702207692800");
            });
            _client.Opened += Client_Ready;
            _client.PacketReceived += Client_PacketReceived;
            _client.Closed += OnClientClosed;
            _client.Error += OnClientError;
            
            await _client.ConnectAsync(new string[] { "ws://127.0.0.1:5050" }, CancellationToken.None, true);

            await Task.Delay(5000);
            while (_client.IsStillAlive)
            {
                Console.WriteLine($"Is Client Alive: {_client.IsStillAlive}");
                await Task.Delay(10000);
            }
        }

        private static async Task OnClientError(object? sender, ConnectionErrorEventArgs args)
        {
            if (_client != null)
            {
                await _client.CloseAsync();
            }
            Console.WriteLine($"Error: {args.Message}");
        }

        private static async Task OnClientClosed(object? sender, ConnectionClosedEventArgs args)
        {
            if (_client != null)
            {
                await _client.CloseAsync();
            }
            Console.WriteLine($"Closed: {args.Description}");
        }

        private static Task Client_PacketReceived(object? sender, PacketReceivedEventArgs args)
        {
            //var con = sender as ByteProtoConnection;
            if (args.Packet is ByteProtoLoginResponse response)
            {
                Console.WriteLine($"login response:{JsonSerializer.Serialize(response)}");
            }

            return Task.CompletedTask;
        }

        private static Task Client_Ready(object? sender)
        {
            if (_client == null)
            {
                return Task.CompletedTask;
            }
            // await _client.SendPacketAsync(new ByteProtoLoginRequest() { Account = "LiHeyHey", Token = Guid.NewGuid().ToString("N") });
            //Executor.RunAfterAsync(TimeSpan.FromSeconds(15), (ct) => _client.SendPacketAsync(new ByteProtoLoginRequest() { Account = "LiHeyHey", Token = Guid.NewGuid().ToString("N") }, ct)).Ignore();
            Debug.WriteLine("");
            return Task.CompletedTask;
        }
    }
}
