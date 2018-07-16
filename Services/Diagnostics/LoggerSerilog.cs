// // Copyright (c) Microsoft. All rights reserved.
//
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using Serilog;
// using Serilog.Sinks.SystemConsole.Themes;
//
// namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
// {
//     public class Logger : ILogger
//     {
//         private readonly string processId;
//         private readonly LogLevel logLevel;
//         private readonly bool logProcessId;
//         private readonly string dateFormat;
//         private readonly object fileLock;
//
//         private readonly bool bwEnabled;
//         private readonly bool blackListEnabled;
//         private readonly bool whiteListEnabled;
//         private readonly bool bwPrefixUsed;
//         private readonly HashSet<string> blackList;
//         private readonly HashSet<string> whiteList;
//         private readonly string bwListPrefix;
//         private readonly int bwListPrefixLength;
//
//         public Logger(string processId) :
//             this(processId, new LoggingConfig())
//         {
//         }
//
//         public Logger(string processId, ILoggingConfig config)
//         {
//             this.processId = processId;
//             this.logLevel = config.LogLevel;
//             this.logProcessId = config.LogProcessId;
//             this.dateFormat = config.DateFormat;
//
//             this.blackList = config.BlackList;
//             this.whiteList = config.WhiteList;
//
//             this.blackListEnabled = this.blackList.Count > 0;
//             this.whiteListEnabled = this.whiteList.Count > 0;
//             this.bwEnabled = this.blackListEnabled || this.whiteListEnabled;
//
//             this.bwPrefixUsed = !string.IsNullOrEmpty(config.BwListPrefix);
//             this.bwListPrefix = config.BwListPrefix;
//             this.bwListPrefixLength = config.BwListPrefix.Length;
//
//             this.fileLock = new object();
//
//             Log.Logger = new LoggerConfiguration()
//                 .MinimumLevel.Debug()
//                 .WriteTo.Console(
//                     theme: SerilogDark,
//                     outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
//                 .CreateLogger();
//         }
//
//         public LogLevel LogLevel => this.logLevel;
//
//         public bool DebugIsEnabled => this.logLevel <= LogLevel.Debug;
//
//         public bool InfoIsEnabled => this.logLevel <= LogLevel.Info;
//
//         public string FormatDate(long time)
//         {
//             return DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(this.dateFormat);
//         }
//
//         // The following 5 methods allow to log a message, capturing the context
//         // (i.e. the method where the log message is generated)
//         
//         public void Write(string message, Action context)
//         {
//             this.Write(LogLevel.Always, context.GetMethodInfo(), message);
//         }
//
//         public void Debug(string message, Action context)
//         {
//             if (this.logLevel > LogLevel.Debug) return;
//             this.Write(LogLevel.Debug, context.GetMethodInfo(), message);
//         }
//
//         public void Info(string message, Action context)
//         {
//             if (this.logLevel > LogLevel.Info) return;
//             this.Write(LogLevel.Info, context.GetMethodInfo(), message);
//         }
//
//         public void Warn(string message, Action context)
//         {
//             if (this.logLevel > LogLevel.Warn) return;
//             this.Write(LogLevel.Warn, context.GetMethodInfo(), message);
//         }
//
//         public void Error(string message, Action context)
//         {
//             if (this.logLevel > LogLevel.Error) return;
//             this.Write(LogLevel.Error, context.GetMethodInfo(), message);
//         }
//
//         // The following 5 methods allow to log a message and some data,
//         // capturing the context (i.e. the method where the log message is generated)
//
//         public void Write(string message, Func<object> context)
//         {
//             if (!string.IsNullOrEmpty(message)) message += ", ";
//             message += Serialization.Serialize(context.Invoke());
//
//             this.Write(LogLevel.Always, context.GetMethodInfo(), message);
//         }
//         
//         public void Debug(string message, Func<object> context)
//         {
//             if (this.logLevel > LogLevel.Debug) return;
//
//             if (!string.IsNullOrEmpty(message)) message += ", ";
//             message += Serialization.Serialize(context.Invoke());
//
//             this.Write(LogLevel.Debug, context.GetMethodInfo(), message);
//         }
//
//         public void Info(string message, Func<object> context)
//         {
//             if (this.logLevel > LogLevel.Info) return;
//
//             if (!string.IsNullOrEmpty(message)) message += ", ";
//             message += Serialization.Serialize(context.Invoke());
//
//             this.Write(LogLevel.Info, context.GetMethodInfo(), message);
//         }
//
//         public void Warn(string message, Func<object> context)
//         {
//             if (this.logLevel > LogLevel.Warn) return;
//
//             if (!string.IsNullOrEmpty(message)) message += ", ";
//             message += Serialization.Serialize(context.Invoke());
//
//             this.Write(LogLevel.Warn, context.GetMethodInfo(), message);
//         }
//
//         public void Error(string message, Func<object> context)
//         {
//             if (this.logLevel > LogLevel.Error) return;
//
//             if (!string.IsNullOrEmpty(message)) message += ", ";
//             message += Serialization.Serialize(context.Invoke());
//
//             this.Write(LogLevel.Error, context.GetMethodInfo(), message);
//         }
//
//         public void LogToFile(string filename, string text)
//         {
//             // Without the lock, some logs would be lost due to contentions
//             lock (this.fileLock)
//             {
//                 File.AppendAllText(filename, text);
//             }
//         }
//
//         /// <summary>
//         /// Log the message and information about the context, cleaning up
//         /// and shortening the class name and method name (e.g. removing
//         /// symbols specific to .NET internal implementation)
//         /// </summary>
//         private void Write(LogLevel level, MethodInfo context, string text)
//         {
//             // Extract the Class Name from the context
//             var classname = "";
//             if (context.DeclaringType != null)
//             {
//                 classname = context.DeclaringType.FullName;
//             }
//
//             classname = classname.Split(new[] { '+' }, 2).First();
//             classname = classname.Split('.').LastOrDefault();
//
//             // Extract the Method Name from the context
//             var methodname = context.Name;
//             methodname = methodname.Split(new[] { '>' }, 2).First();
//             methodname = methodname.Split(new[] { '<' }, 2).Last();
//
//             // Check blacklisted and whitelisted classes and methods
//             if (this.bwEnabled)
//             {
//                 var bwClass = classname;
//                 if (this.bwPrefixUsed && bwClass.StartsWith(this.bwListPrefix))
//                 {
//                     bwClass = bwClass.Substring(this.bwListPrefixLength);
//                 }
//
//                 if (this.blackListEnabled
//                     && (this.blackList.Contains(bwClass + "." + methodname)
//                         || this.blackList.Contains(bwClass + ".*")))
//                 {
//                     return;
//                 }
//
//                 if (this.whiteListEnabled
//                     && !this.whiteList.Contains(bwClass + "." + methodname)
//                     && !this.whiteList.Contains(bwClass + ".*"))
//                 {
//                     return;
//                 }
//             }
//
//             var logEntry = this.logProcessId
//                 ? $"[{this.processId}][{classname}:{methodname}] {text}"
//                 : $"[{classname}:{methodname}] {text}";
//             switch (level)
//             {
//                 case LogLevel.Debug:
//                     Log.Debug(logEntry);
//                     break;
//                 case LogLevel.Info:
//                     Log.Information(logEntry);
//                     break;
//                 case LogLevel.Warn:
//                     Log.Warning(logEntry);
//                     break;
//                 case LogLevel.Error:
//                     Log.Error(logEntry);
//                     break;
//             }
//         }
//
//         private static SystemConsoleTheme SerilogDark { get; } = SystemConsoleTheme.Colored;
//
//         private static SystemConsoleTheme SerilogLight { get; } =
//             new SystemConsoleTheme(
//                 new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
//                 {
//                     [ConsoleThemeStyle.Text] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.DarkGray,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.SecondaryText] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Gray,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.TertiaryText] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Gray,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.Invalid] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.DarkYellow,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.Null] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Black,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.Name] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Black,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.String] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Black,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.Number] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Black,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.Boolean] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Black,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.Scalar] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Black,
//                         Background = ConsoleColor.White
//                     },
//                     [ConsoleThemeStyle.LevelVerbose] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Gray,
//                         Background = ConsoleColor.DarkGray
//                     },
//                     [ConsoleThemeStyle.LevelDebug] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.White,
//                         Background = ConsoleColor.DarkGray
//                     },
//                     [ConsoleThemeStyle.LevelInformation] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.White,
//                         Background = ConsoleColor.Blue
//                     },
//                     [ConsoleThemeStyle.LevelWarning] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.Gray,
//                         Background = ConsoleColor.Yellow
//                     },
//                     [ConsoleThemeStyle.LevelError] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.White,
//                         Background = ConsoleColor.Red
//                     },
//                     [ConsoleThemeStyle.LevelFatal] = new SystemConsoleThemeStyle
//                     {
//                         Foreground = ConsoleColor.White,
//                         Background = ConsoleColor.Red
//                     }
//                 });
//     }
// }
