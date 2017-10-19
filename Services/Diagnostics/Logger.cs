// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public enum LogLevel
    {
        Debug = 10,
        Info = 20,
        Warn = 30,
        Error = 40
    }

    public interface ILogger
    {
        LogLevel LogLevel { get; }

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)

        void Debug(string message, Action context);
        void Info(string message, Action context);
        void Warn(string message, Action context);
        void Error(string message, Action context);

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)

        void Debug(string message, Func<object> context);
        void Info(string message, Func<object> context);
        void Warn(string message, Func<object> context);
        void Error(string message, Func<object> context);
    }

    public class Logger : ILogger
    {
        private readonly string processId;
        private readonly LogLevel logLevel;

        public Logger(string processId, LogLevel logLevel)
        {
            this.processId = processId;
            this.logLevel = logLevel;
        }

        public LogLevel LogLevel => this.logLevel;

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
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

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
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

            var time = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Console.WriteLine($"[{level}][{time}][{this.processId}][{classname}:{methodname}] {text}");
        }
    }
}
