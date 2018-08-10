// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface ILogger
    {
        LogLevel LogLevel { get; }

        string FormatDate(long time);

        bool DebugIsEnabled { get; }
        bool InfoIsEnabled { get; }

        // The following 5 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)

        // Use "Write()" to write the message regardless of the log level, e.g. at startup
        void Write(string message, Action context);

        void Debug(string message, Action context);
        void Info(string message, Action context);
        void Warn(string message, Action context);
        void Error(string message, Action context);

        // The following 5 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)

        // Use "Write()" to write the message regardless of the log level, e.g. at startup
        void Write(string message, Func<object> context);

        void Debug(string message, Func<object> context);
        void Info(string message, Func<object> context);
        void Warn(string message, Func<object> context);
        void Error(string message, Func<object> context);

        void LogToFile(string filename, string text);
    }

    public class Logger : ILogger
    {
        private readonly string processId;
        private readonly LogLevel logLevel;
        private readonly bool logProcessId;
        private readonly string dateFormat;
        private readonly object fileLock;

        private readonly bool bwEnabled;
        private readonly bool blackListEnabled;
        private readonly bool whiteListEnabled;
        private readonly bool bwPrefixUsed;
        private readonly HashSet<string> blackList;
        private readonly HashSet<string> whiteList;
        private readonly string bwListPrefix;
        private readonly int bwListPrefixLength;

        public Logger(string processId) :
            this(processId, new LoggingConfig())
        {
        }

        public Logger(string processId, ILoggingConfig config)
        {
            this.processId = processId;
            this.logLevel = config.LogLevel;
            this.logProcessId = config.LogProcessId;
            this.dateFormat = config.DateFormat;

            this.blackList = config.BlackList;
            this.whiteList = config.WhiteList;

            this.blackListEnabled = this.blackList.Count > 0;
            this.whiteListEnabled = this.whiteList.Count > 0;
            this.bwEnabled = this.blackListEnabled || this.whiteListEnabled;

            this.bwPrefixUsed = !string.IsNullOrEmpty(config.BwListPrefix);
            this.bwListPrefix = config.BwListPrefix;
            this.bwListPrefixLength = config.BwListPrefix.Length;

            this.fileLock = new object();
        }

        public LogLevel LogLevel => this.logLevel;

        public bool DebugIsEnabled => this.logLevel <= LogLevel.Debug;

        public bool InfoIsEnabled => this.logLevel <= LogLevel.Info;

        public string FormatDate(long time)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(this.dateFormat);
        }

        // The following 5 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)

        public void Write(string message, Action context)
        {
            this.Write("---", context.GetMethodInfo(), message);
        }

        public void Debug(string message, Action context)
        {
            if (this.logLevel > LogLevel.Debug) return;
            this.Write("DEBUG", context.GetMethodInfo(), message);
        }

        public void Info(string message, Action context)
        {
            if (this.logLevel > LogLevel.Info) return;
            this.Write("INFO", context.GetMethodInfo(), message);
        }

        public void Warn(string message, Action context)
        {
            if (this.logLevel > LogLevel.Warn) return;
            this.Write("WARN", context.GetMethodInfo(), message);
        }

        public void Error(string message, Action context)
        {
            if (this.logLevel > LogLevel.Error) return;
            this.Write("ERROR", context.GetMethodInfo(), message);
        }

        // The following 5 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)

        public void Write(string message, Func<object> context)
        {
            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write("---", context.GetMethodInfo(), message);
        }

        public void Debug(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write("DEBUG", context.GetMethodInfo(), message);
        }

        public void Info(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write("INFO", context.GetMethodInfo(), message);
        }

        public void Warn(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write("WARN", context.GetMethodInfo(), message);
        }

        public void Error(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write("ERROR", context.GetMethodInfo(), message);
        }

        public void LogToFile(string filename, string text)
        {
            // Without the lock, some logs would be lost due to contentions
            lock (this.fileLock)
            {
                File.AppendAllText(filename, text);
            }
        }

        /// <summary>
        /// Log the message and information about the context, cleaning up
        /// and shortening the class name and method name (e.g. removing
        /// symbols specific to .NET internal implementation)
        /// </summary>
        private void Write(string level, MethodInfo context, string text)
        {
            // Extract the Class Name from the context
            var classname = "";
            if (context.DeclaringType != null)
            {
                classname = context.DeclaringType.FullName;
            }

            classname = classname.Split(new[] { '+' }, 2).First();
            classname = classname.Split('.').LastOrDefault();

            // Extract the Method Name from the context
            var methodname = context.Name;
            methodname = methodname.Split(new[] { '>' }, 2).First();
            methodname = methodname.Split(new[] { '<' }, 2).Last();

            // Check blacklisted and whitelisted classes and methods
            if (this.bwEnabled)
            {
                var bwClass = classname;
                if (this.bwPrefixUsed && bwClass.StartsWith(this.bwListPrefix))
                {
                    bwClass = bwClass.Substring(this.bwListPrefixLength);
                }

                if (this.blackListEnabled
                    && (this.blackList.Contains(bwClass + "." + methodname)
                        || this.blackList.Contains(bwClass + ".*")))
                {
                    return;
                }

                if (this.whiteListEnabled
                    && !this.whiteList.Contains(bwClass + "." + methodname)
                    && !this.whiteList.Contains(bwClass + ".*"))
                {
                    return;
                }
            }

            var time = DateTimeOffset.UtcNow.ToString(this.dateFormat);

            Console.WriteLine(this.logProcessId
                ? $"[{level}][{time}][{this.processId}][{classname}:{methodname}] {text}"
                : $"[{level}][{time}][{classname}:{methodname}] {text}");
        }
    }
}
