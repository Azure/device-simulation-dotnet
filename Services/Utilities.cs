using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public static class Utilities
    {
        static Dictionary<Tuple<string, string, string>, MethodInfo> methodDictionary = new Dictionary<Tuple<string, string, string>, MethodInfo>();

         /// <summary>
        /// Extends the System.Type-type to search for a given extended MethodeName.
        /// </summary>
        /// <param name="MethodeName">Name of the Methode</param>
        /// <returns>the found Methode or null</returns>
        public static MethodInfo GetExtensionMethod(string typeName, string assemblyName, string MethodeName)
        {
            Tuple<string, string, string> key = new Tuple<string, string, string>(typeName, assemblyName, MethodeName);

            if (methodDictionary.ContainsKey(key))
            {
                return methodDictionary[key];
            }

            List<Type> AssTypes = new List<Type>();

            Assembly gpAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
            AssTypes.AddRange(gpAssembly.GetTypes());

            var query = from type in AssTypes
                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        where method.GetParameters()[0].ParameterType.Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)
                        select method;

            var mi = from methode in query.ToArray<MethodInfo>()
                     where methode.Name == MethodeName
                     select methode;

            if (mi.Count<MethodInfo>() <= 0)
            {
                return null;
            }
            else
            {
                methodDictionary[key] = mi.First<MethodInfo>();
                return methodDictionary[key];
            }
        }
    }
}
