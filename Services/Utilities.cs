// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public static class Utilities
    {
        static readonly Dictionary<Tuple<string, string, string>, MethodInfo> methodDictionary = new Dictionary<Tuple<string, string, string>, MethodInfo>();

        /// <summary>
        /// Extends the System.Type-type to search for a given extended Method Name.
        /// </summary>
        /// <param name="baseTypeName">Name of the base type</param>
        /// <param name="assemblyName">Name of the assembly</param>
        /// <param name="methodName">Name of the Method</param>
        /// <returns>the found Method or null</returns>
        public static MethodInfo GetExtensionMethod(string baseTypeName, string assemblyName, string methodName)
        {
            if (string.IsNullOrEmpty(baseTypeName) || string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException();
            }

            Tuple<string, string, string> key = new Tuple<string, string, string>(baseTypeName, assemblyName, methodName);

            //Check if the method info is already available on cached dictionary
            if (methodDictionary.ContainsKey(key))
            {
                return methodDictionary[key];
            }

            List<Type> assemblyTypes = new List<Type>();

            Assembly matchingAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
            assemblyTypes.AddRange(matchingAssembly.GetTypes());

            var query = from type in assemblyTypes
                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        where method.GetParameters()[0].ParameterType.Name.Equals(baseTypeName, StringComparison.InvariantCultureIgnoreCase)
                        select method;

            var methodInfo = from method in query.ToArray<MethodInfo>()
                             where method.Name == methodName
                             select method;

            if (methodInfo.Count<MethodInfo>() <= 0)
            {
                return null;
            }
            else
            {
                // Cache it in Dictionary, for repeating future calls
                methodDictionary[key] = methodInfo.First<MethodInfo>();
                return methodInfo.First<MethodInfo>();
            }
        }
    }
}
