namespace ByteFlow.Connection
{
    public record PacketReceivedEventArgs
    {
        public object? Packet { get; }

        internal PacketReceivedEventArgs(object? packet)
        {
            this.Packet = packet;
        }
    }
}
