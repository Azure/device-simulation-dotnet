// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures
{
    public static class ListExtensions
    {
        // Note: fields marked with [ThreadStatic] must be static and not initialized statically.
        [ThreadStatic]
        private static Random rnd;

        // Thread safe random generator. 
        private static Random Rnd => rnd ?? (rnd = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));

        // Fisher-Yates shuffle
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list.Count < 2) return;

            var cursor = list.Count;

            while (cursor-- > 1)
            {
                var randomPosition = Rnd.Next(cursor + 1);
                var value = list[randomPosition];
                list[randomPosition] = list[cursor];
                list[cursor] = value;
            }
        }
    }
}
