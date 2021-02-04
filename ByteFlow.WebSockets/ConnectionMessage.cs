namespace ByteFlow.WebSockets
{
    public sealed class ConnectionMessage
    {
        public ConnectionMessageType Type { get; }

        public byte[]? Binary { get; }

        public string Text { get; }

        internal ConnectionMessage(ConnectionMessageType type, string text, byte[]? bytes = null)
        {
            this.Type = type;
            this.Text = text;
            this.Binary = bytes;
        }
    }
}
