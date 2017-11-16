// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Concurrency
{
    public class RatedCounterTest
    {
        /**
         * Checks the configuration validation
         * Also covers https://github.com/Azure/device-simulation-dotnet/issues/122
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntAllowLowCounterRate()
        {
            Assert.Throws<InvalidConfigurationException>(() => new BadCounter1(new Mock<ILogger>().Object));
            Assert.Throws<InvalidConfigurationException>(() => new BadCounter2(new Mock<ILogger>().Object));
        }

        class BadCounter1 : RatedCounter
        {
            public BadCounter1(ILogger logger)
                : base(1, 999, "BadCounter1", logger)
            {
            }
        }

        class BadCounter2 : RatedCounter
        {
            public BadCounter2(ILogger logger)
                : base(0, 99999, "BadCounter2", logger)
            {
            }
        }
    }
}
