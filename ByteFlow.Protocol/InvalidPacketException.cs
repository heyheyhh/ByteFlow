using System;

namespace ByteFlow.Protocol
{
    public class InvalidPacketException : Exception
    {
        public InvalidPacketException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
