// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    /// <summary>
    /// Provide factory pattern for dependencies that are instantiated
    /// multiple times during the application lifetime.
    /// How to use:
    /// <code>
    /// class MyClass : IMyClass {
    ///     public MyClass(IFactory factory) {
    ///         this.factory = factory;
    ///     }
    ///     public SomeMethod() {
    ///         var instance1 = this.factory.Resolve<ISomething>();
    ///         var instance2 = this.factory.Resolve<ISomething>();
    ///         var instance3 = this.factory.Resolve<ISomething>();
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IFactory
    {
        T Resolve<T>();
    }

    public class Factory : IFactory
    {
        private static Func<Type, object> resolver;

        public T Resolve<T>()
        {
            return (T)resolver.Invoke(typeof(T));
        }

        public static void RegisterResolver(Func<Type, object> func)
        {
            resolver = func;
        }
    }
}
