using System;
using System.Text;

namespace ByteFlow.Exceptions
{
    public abstract class ExceptionAbstract : Exception
    {
        /// <summary>
        /// 业务标记
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// 意图消息：即用于描述抛出此异常的意图的消息。仅仅用于表达意图，不会像<see cref="ToString"/>那么详细
        /// </summary>
        public virtual string IntentMessage { get; } = string.Empty;

        private readonly string stackTrace;

        public ExceptionAbstract(int skipFrames = 0)
               : base(string.Empty)
            => this.stackTrace = StackTraceExtension.GetStackTrace(skipFrames);

        public ExceptionAbstract(string message, int skipFrames = 0)
            : base(message)
            => this.stackTrace = StackTraceExtension.GetStackTrace(skipFrames);

        public ExceptionAbstract(string message, Exception innerException, int skipFrames = 0)
            : base(message, innerException)
            => this.stackTrace = StackTraceExtension.GetStackTrace(skipFrames);

        public override string StackTrace => this.stackTrace != null ? this.stackTrace.Trim('\r', '\n', ' ') : string.Empty;

        public override string ToString() => ExceptionFormatter.Format(sb => this.BuildString(sb));

        protected virtual void BuildString(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"type: {this.GetType().FullName}");

            if (!string.IsNullOrWhiteSpace(this.Tags))
            {
                stringBuilder.AppendLine($"tags: {this.Tags}");
            }

            if (!string.IsNullOrWhiteSpace(this.Message))
            {
                stringBuilder.AppendLine($"message: {this.Message}");
            }

            if (!string.IsNullOrWhiteSpace(this.IntentMessage))
            {
                stringBuilder.AppendLine($"intentMsg: {this.IntentMessage}");
            }

            stringBuilder.AppendLine($"stackTrace: {this.StackTrace}");
        }
    }
}
