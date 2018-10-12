// Copyright (c) Microsoft. All rights reserved.

using SimulationAgent.Test.helpers;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads;
using Moq;
using Xunit;

namespace SimulationAgent.Test.SimulationThreads
{
    public class DeviceTelemetryTaskTest
    {
        private static int DEVICE_COUNT = 100;
        private static int TELEMETRY_THREAD_COUNT = 3;

        private readonly ConcurrentDictionary<string, Mock<IDeviceTelemetryActor>> deviceTelemetryActorMocks;
        private readonly ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActorObjects;

        private readonly DeviceTelemetryTask target;

        public DeviceTelemetryTaskTest()
        {
            Mock<ISimulationConcurrencyConfig> mockSimulationConcurrencyConfig = new Mock<ISimulationConcurrencyConfig>();
            Mock<ILogger> mockLogger = new Mock<ILogger>();

            this.target = new DeviceTelemetryTask(
                mockSimulationConcurrencyConfig.Object,
                mockLogger.Object);
        }

        [Fact]
        public void ItCallsRunAsyncOnTheExpectedChunk()
        {
            // Arrange
            var threadPosition = 1;
            var cancellationToken = new CancellationTokenSource();
            this.BuildMockDeviceActors(
                this.deviceTelemetryActorMocks,
                this.deviceTelemetryActorObjects,
                DEVICE_COUNT,
                cancellationToken);

            // The target will send device telemetry for all devices outside of a window defined
            // by 'firstActor' and 'lastActor'. Here we're setting up the expected values of these
            // indices. 
            int chunkSize = (int)Math.Ceiling((double)DEVICE_COUNT / (double)TELEMETRY_THREAD_COUNT);
            var firstActor = chunkSize * (threadPosition - 1);
            var lastActor = Math.Min(chunkSize * threadPosition, DEVICE_COUNT);

            // Act
            // The cancellation token will be triggered through a callback,
            // so that the main loop in the target will only run once.
            var targetTask = this.target.RunAsync(
                this.deviceTelemetryActorObjects,
                1,
                TELEMETRY_THREAD_COUNT,
                cancellationToken.Token);
            targetTask.Wait(Constants.TEST_TIMEOUT);

            // Assert
            // Verify that RunAsync was called on the expected actors
        }

        /*
         * Because the target accepts a collection of IDeviceTelemetryActor type, when we pass
         * this dictionary to the target, we will not be able to reference the mocks later
         * (because we're passing a collection of typed objects, not the mocks). This method
         * will generate two collections: one of mocks and one of objects. The second one will
         * be passed to the target, and later, the first one will be used to validate usage.
         */
        private void BuildMockDeviceActors(
            ConcurrentDictionary<string, Mock<IDeviceTelemetryActor>> mockDictionary,
            ConcurrentDictionary<string, IDeviceTelemetryActor> objectDictionary,
            int count,
            CancellationTokenSource cancellationToken)
        {
            mockDictionary.Clear();
            objectDictionary.Clear();

            for (int i = 0; i < count; i++)
            {
                var deviceName = $"device_{i}";
                var mockActor = new Mock<IDeviceTelemetryActor>();

                // Cancel the token so that the main loop of the target will stop
                // after the first iteration
                mockActor.Setup(x => x.RunAsync())
                    .Callback(() =>
                    {
                        cancellationToken.Cancel();
                    });

                mockDictionary.TryAdd(deviceName, mockActor);
                objectDictionary.TryAdd(deviceName, mockActor.Object);
            }
        }
    }
}
