using System.Net.WebSockets;

namespace ByteFlow.WebSockets.Events
{
    public sealed class ConnectionClosedEventArgs
    {
        public WebSocketCloseStatus Status { get; }

        public string Description { get; }

        public bool ClosedByRemote { get; }

        public ConnectionClosedEventArgs(WebSocketCloseStatus status, string? desc = null, bool isClosedByRemote = false)
        {
            this.Status = status;
            this.Description = desc ?? string.Empty;
            this.ClosedByRemote = isClosedByRemote;
        }
    }
}
