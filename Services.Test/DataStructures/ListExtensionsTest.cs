using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Xunit;

namespace Services.Test.DataStructures
{
    public class ListExtensionsTest
    {
        [Fact]
        void ItShufflesAList()
        {
            // Arrange
            var unshuffled = new List<int>() { 1, 2, 3, 4, 5 };

            // Act
            var shuffled = new List<int>(unshuffled);
            shuffled.Shuffle();

            // Assert
            Assert.Equal(unshuffled.Count, shuffled.Count);
            Assert.True(shuffled.Count > 0);
            var matches = 0;
            var cursor = unshuffled.Count;
            while (cursor-- > 1)
            {
                if (unshuffled[cursor] == shuffled[cursor])
                {
                    matches++;
                }
            }

            Assert.True(matches < unshuffled.Count);
        }
    }
}
