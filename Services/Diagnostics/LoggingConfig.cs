// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public enum LogLevel
    {
        Debug = 10,
        Info = 20,
        Warn = 30,
        Error = 40
    }
    
    public interface ILoggingConfig
    {
        LogLevel LogLevel { get; }
        bool LogProcessId { get; }
        bool ExtraDiagnostics { get; }
        string ExtraDiagnosticsPath { get; }
        string DateFormat { get; }
        string BwListPrefix { get; }
        HashSet<string> BlackList { get; }
        HashSet<string> WhiteList { get; }
    }
    
    public class LoggingConfig : ILoggingConfig
    {
        public const LogLevel DEFAULT_LOGLEVEL = LogLevel.Warn;
        public const string DEFAULT_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";
        
        public LogLevel LogLevel { get; set; }
        public bool LogProcessId { get; set; }
        public bool ExtraDiagnostics { get; set; }
        public string ExtraDiagnosticsPath { get; set; }
        public string DateFormat { get; set; }
        public string BwListPrefix { get; set; }
        public HashSet<string> BlackList { get; set; }
        public HashSet<string> WhiteList { get; set; }

        public LoggingConfig()
        {
            this.LogLevel = DEFAULT_LOGLEVEL;
            this.LogProcessId = true;
            this.ExtraDiagnostics = false;
            this.DateFormat = DEFAULT_DATE_FORMAT;
            this.BwListPrefix = string.Empty;
            this.BlackList = new HashSet<string>();
            this.WhiteList = new HashSet<string>();
        }
    }
}
