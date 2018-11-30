﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public static class Serialization
    {
        // Save memory avoiding serializations that go too deep
        private static readonly JsonSerializerSettings serializationSettings =
            new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                MaxDepth = 4
            };

        public static string Serialize(object o)
        {
            var logdata = new Dictionary<string, object>();

            // To avoid flooding the logs and logging exceptions, filter
            // exceptions' data and log only what's useful
            foreach (PropertyInfo data in o.GetType().GetRuntimeProperties())
            {
                var name = data.Name;
                var value = data.GetValue(o, index: null);

                if (value is Exception e)
                {
                    logdata.Add(name, SerializeException(e));
                }
                else if (value is double d)
                {
                    d = (long) (d * 1000);
                    d = d / 1000;
                    logdata.Add(name, d);
                }
                else
                {
                    logdata.Add(name, value);
                }
            }

            return JsonConvert.SerializeObject(logdata, serializationSettings);
        }

        public static object SerializeException(Exception e, int depth = 4)
        {
            if (e == null) return null;
            if (depth == 0) return "-max serialization depth reached-";

            var exception = e as AggregateException;
            if (exception != null)
            {
                var innerExceptions = exception.InnerExceptions
                    .Select(ie => SerializeException(ie, depth - 1)).ToList();

                return new
                {
                    ExceptionFullName = exception.GetType().FullName,
                    ExceptionMessage = exception.Message,
                    exception.StackTrace,
                    exception.Source,
                    exception.Data,
                    InnerExceptions = innerExceptions
                };
            }

            return new
            {
                ExceptionFullName = e.GetType().FullName,
                ExceptionMessage = e.Message,
                e.StackTrace,
                e.Source,
                e.Data,
                InnerException = SerializeException(e.InnerException, depth - 1)
            };
        }
    }
}
