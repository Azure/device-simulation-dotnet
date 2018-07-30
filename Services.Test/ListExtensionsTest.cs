// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class ListExtensionsTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItShufflesWithoutExceptions()
        {
            var list = new List<string>();
            for (int i = 0; i < 100; i++) list.Shuffle();

            list.Add("1");
            for (int i = 0; i < 100; i++) list.Shuffle();

            list.Add("2");
            for (int i = 0; i < 100; i++) list.Shuffle();

            list.Add("3");
            for (int i = 0; i < 100; i++) list.Shuffle();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItShuffles()
        {
            // Arrange
            var list = new List<string> { "1", "2", "3", "4" };

            // Act
            var combos = new HashSet<string>();
            for (int i = 0; i < 2000; i++)
            {
                list.Shuffle();
                var combo = string.Join("", list);
                combos.Add(combo);
            }

            // Assert
            Assert.True(combos.Count > 10);
        }
    }
}
