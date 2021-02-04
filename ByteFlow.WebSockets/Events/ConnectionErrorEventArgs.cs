using System;

namespace ByteFlow.WebSockets.Events
{
    public sealed class ConnectionErrorEventArgs
    {
        public string Message { get; }

        public Exception? Exception { get; }

        public ConnectionErrorEventArgs(string msg, Exception? exp = null)
        {
            this.Message = msg;
            this.Exception = exp;
        }
    }
}
