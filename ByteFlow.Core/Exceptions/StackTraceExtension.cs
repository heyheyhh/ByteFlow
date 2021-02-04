using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace ByteFlow.Exceptions
{
    public static class StackTraceExtension
    {
        private static readonly HashSet<string> IgnoredAssemblies = new HashSet<string>()
        {
            "Microsoft.*",
            "System.*",
            "Orleans.*"
        };

        private static readonly HashSet<string> IgnoredNamespaces = new HashSet<string>();

        public static void AddIgnoreAssemblies(params string[] assemblyNames)
        {
            if (assemblyNames == null || assemblyNames.Length <= 0)
            {
                return;
            }

            foreach (var ass in assemblyNames)
            {
                IgnoredAssemblies.Add(ass);
            }
        }

        public static void AddIgnoredNamespace(params string[] namespaces)
        {
            if (namespaces == null || namespaces.Length <= 0)
            {
                return;
            }

            foreach (var ns in namespaces)
            {
                IgnoredNamespaces.Add(ns);
            }
        }

        public static void CancelIngoreAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return;
            }

            IgnoredAssemblies.Remove(assemblyName);
        }

        /// <summary>
        /// 获取调用栈信息
        /// </summary>
        /// <param name="skip">跳过的帧数，默认为0</param>
        /// <returns>调用栈信息</returns>
        public static string GetStackTrace(int skip = 0)
        {
            var stackTrace = new StackTrace(true);
            var stackFrames = stackTrace.GetFrames();
            (int start, int end) = FindUserStackFrameIndexes(stackFrames);
            if (start < 0 || end < 0 || start > end || start >= stackFrames.Length || end >= stackFrames.Length)
            {
                return string.Empty;
            }

            start += skip;

            string outStr = string.Empty;
            if (start == end)
            {
                var frame = stackFrames[start];
                if (frame != null)
                {
                    outStr = $"->{new StackTrace(frame).ToString().Trim('\r', '\n', ' ')}";
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = start; i <= end; i++)
                {
                    var frame = stackFrames[i];
                    if (frame == null)
                    {
                        continue;
                    }

                    sb.AppendLine($"->{new StackTrace(frame).ToString().Trim('\r', '\n', ' ')}");
                }

                outStr = sb.ToString();
            }

            return outStr;
        }

        public static string GetStackTrace(Exception ex, int skip = 0)
        {
            var stackTrace = new StackTrace(ex, true);
            var stackFrames = stackTrace.GetFrames();
            if (stackFrames == null || stackFrames.Length <= 0)
            {
                return string.Empty;
            }

            (int start, int end) = FindUserStackFrameIndexes(stackFrames);
            if (start < 0 || end < 0 || start > end || start >= stackFrames.Length || end >= stackFrames.Length)
            {
                return string.Empty;
            }

            start += skip;

            string outStr = string.Empty;
            if (start == end)
            {
                var frame = stackFrames[start];
                if (frame != null)
                {
                    outStr = $"->{new StackTrace(frame).ToString().TrimStart(' ')}";
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = start; i <= end; i++)
                {
                    var frame = stackFrames[i];
                    if (frame == null)
                    {
                        continue;
                    }

                    sb.AppendLine($"->{new StackTrace(frame).ToString().TrimStart(' ')}");
                }

                outStr = sb.ToString();
            }

            return outStr;
        }

        private static (int start, int end) FindUserStackFrameIndexes(StackFrame?[] stackFrames)
        {
            if (stackFrames == null || stackFrames.Length == 0)
            {
                return (-1, -1);
            }

            int firstUserStackFrame = -1;
            int lastUserStackFrame = -1;
            for (int i = 0; i < stackFrames.Length; ++i)
            {
                var stackFrame = stackFrames[i];
                if (stackFrame == null)
                {
                    continue;
                }

                if (stackFrame.GetILOffset() == StackFrame.OFFSET_UNKNOWN || string.IsNullOrEmpty(stackFrame.GetFileName()))
                {
                    break;
                }

                var method = stackFrame.GetMethod();
                if (IsNamespaceIgnored(method))
                {
                    continue;
                }

                if (IsAssemblyIgnored(method?.DeclaringType?.Assembly ?? method?.Module?.Assembly))
                {
                    continue;
                }

                lastUserStackFrame = i;

                if (firstUserStackFrame < 0)
                {
                    firstUserStackFrame = i;
                }
            }

            return (firstUserStackFrame, lastUserStackFrame);
        }

        private static bool IsNamespaceIgnored(MethodBase? method)
        {
            if (method == null || method.DeclaringType == null || string.IsNullOrWhiteSpace(method.DeclaringType.Namespace))
            {
                return false;
            }

            var ns = method.DeclaringType.Namespace;
            foreach (var item in IgnoredNamespaces)
            {
                if (item.EndsWith("*"))
                {
                    if (ns.StartsWith(item.TrimEnd('*'), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (item.Equals(ns, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAssemblyIgnored(Assembly? assembly)
        {
            if (assembly == null)
            {
                return true;
            }

            var name = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            foreach (var item in IgnoredAssemblies)
            {
                if (item.EndsWith("*"))
                {
                    if (name.StartsWith(item.TrimEnd('*'), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (item.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
