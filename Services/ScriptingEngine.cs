// Copyright (c) Microsoft. All rights reserved.

using System;
using Jint;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IScriptingEngine
    {
        string Execute(string script, string deviceId, long frequency);
    }

    public class ScriptingEngine : IScriptingEngine
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
        private readonly Engine engine;

        public ScriptingEngine()
        {
            this.engine = new Engine().SetValue("log", new Action<object>(Console.WriteLine));
        }

        public string Execute(string script, string deviceId, long frequency)
        {
            var currentTime = DateTimeOffset.UtcNow.ToString(DateFormat);

            this.engine.Execute(@"
              function hello() {
                log('Hello World');
              };
              hello();
            ");

            var square = new Engine()
                    .SetValue("x", 3) // define a new variable
                    .Execute("x * x") // execute a statement
                    .GetCompletionValue() // get the latest statement completion value
                    .ToObject() // converts the value to .NET
                ;

            return square.ToString();
        }
    }
}
