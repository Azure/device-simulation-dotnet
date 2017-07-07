// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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
        private readonly LogLevel loggingLevel;

        // Save memory avoiding serializations that go too deep
        private static readonly JsonSerializerSettings serializationSettings =
            new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                MaxDepth = 4
            };

        public Logger(string processId, LogLevel loggingLevel)
        {
            this.processId = processId;
            this.loggingLevel = loggingLevel;
        }

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        public void Debug(string message, Action context)
        {
            if (this.loggingLevel > LogLevel.Debug) return;
            this.Write("DEBUG", context.GetMethodInfo(), message);
        }

        public void Info(string message, Action context)
        {
            if (this.loggingLevel > LogLevel.Info) return;
            this.Write("INFO", context.GetMethodInfo(), message);
        }

        public void Warn(string message, Action context)
        {
            if (this.loggingLevel > LogLevel.Warn) return;
            this.Write("WARN", context.GetMethodInfo(), message);
        }

        public void Error(string message, Action context)
        {
            if (this.loggingLevel > LogLevel.Error) return;
            this.Write("ERROR", context.GetMethodInfo(), message);
        }

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
        public void Debug(string message, Func<object> context)
        {
            if (this.loggingLevel > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialize(context.Invoke());

            this.Write("DEBUG", context.GetMethodInfo(), message);
        }

        public void Info(string message, Func<object> context)
        {
            if (this.loggingLevel > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialize(context.Invoke());

            this.Write("INFO", context.GetMethodInfo(), message);
        }

        public void Warn(string message, Func<object> context)
        {
            if (this.loggingLevel > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialize(context.Invoke());

            this.Write("WARN", context.GetMethodInfo(), message);
        }

        public void Error(string message, Func<object> context)
        {
            if (this.loggingLevel > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialize(context.Invoke());

            this.Write("ERROR", context.GetMethodInfo(), message);
        }

        private static string Serialize(object o)
        {
            return JsonConvert.SerializeObject(o, serializationSettings);
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

            var time = DateTimeOffset.UtcNow.ToString("u");
            Console.WriteLine($"[{this.processId}][{time}][{level}][{classname}:{methodname}] {text}");
        }
    }
}
