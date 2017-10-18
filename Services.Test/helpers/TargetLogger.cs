﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Xunit.Abstractions;

namespace Services.Test.helpers
{
    /// <summary>
    /// Use this logger to capture diagnostics data emitted by the
    /// system under test (aka target)
    /// </summary>
    public class TargetLogger : ILogger
    {
        private readonly ITestOutputHelper testLogger;

        public TargetLogger(ITestOutputHelper testLogger)
        {
            this.testLogger = testLogger;
        }

        public LogLevel LogLevel { get; }

        public void Debug(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message);
        }

        public void Warn(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message);
        }

        public void Info(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message);
        }

        public void Error(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message);
        }

        public void Debug(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Info(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Warn(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Error(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        private static string Time()
        {
            return DateTimeOffset.UtcNow.ToString("[HH:mm:ss.fff] ");
        }
    }
}
