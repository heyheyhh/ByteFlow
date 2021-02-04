using Microsoft.Extensions.ObjectPool;
using System;
using System.Text;

namespace ByteFlow.Exceptions
{
    public static class ExceptionFormatter
    {
        private static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

        public static string Format(Exception? exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            if (exception is ExceptionAbstract)
            {
                return exception.ToString();
            }
            else
            {
                return Format(sb =>
                {
                    sb.AppendLine($"message: {exception.Message}");
                    var trace = StackTraceExtension.GetStackTrace(exception);
                    var stackTrace = string.IsNullOrWhiteSpace(trace) ? exception.StackTrace : trace;
                    sb.AppendLine($"stackTrace: {stackTrace?.Trim('\r', '\n', ' ')}");
                });
            }
        }

        public static string Format(Action<StringBuilder> formatAction)
        {
            StringBuilder sb = StringBuilderPool.Get();
            try
            {
                sb.Clear();
                formatAction(sb);
                return sb.ToString();
            }
            finally
            {
                StringBuilderPool.Return(sb);
            }
        }

        public static string GetExceptionMessage(Exception? exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var intentMsg = exception is ExceptionAbstract pe ? pe.IntentMessage : string.Empty;
            var msg = $"msg:{exception.Message.Trim()}, intent msg:{intentMsg}";

            return msg;
        }

        public static string FormatToString(this Exception ex) => Format(ex);
    }
}
