// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SimulationAgent.Test.helpers;
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
        private static int ACTOR_COUNT = 100;
        private static int TELEMETRY_THREAD_COUNT = 3;
        private static int MAX_PENDING_TELEMETRY_TASKS = 10;

        private readonly ConcurrentDictionary<string, Mock<IDeviceTelemetryActor>> deviceTelemetryActorMocks;
        private readonly ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActorObjects;

        private readonly DeviceTelemetryTask target;

        public DeviceTelemetryTaskTest()
        {
            this.deviceTelemetryActorMocks = new ConcurrentDictionary<string, Mock<IDeviceTelemetryActor>>();
            this.deviceTelemetryActorObjects = new ConcurrentDictionary<string, IDeviceTelemetryActor>();

            Mock<IAppConcurrencyConfig> mockSimulationConcurrencyConfig = new Mock<IAppConcurrencyConfig>();
            mockSimulationConcurrencyConfig.SetupGet(x => x.MaxPendingTelemetry).Returns(MAX_PENDING_TELEMETRY_TASKS);
            mockSimulationConcurrencyConfig.SetupGet(x => x.MinDeviceTelemetryLoopDuration).Returns(0);
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
                ACTOR_COUNT,
                cancellationToken);

            // Determine the chunk size so that we'll know how many actors should have been called in
            // one loop of the target's main thread (the expected count is the total number - chunk size).
            int chunkSize = (int) Math.Ceiling((double) ACTOR_COUNT / (double) TELEMETRY_THREAD_COUNT);

            // Act
            // The cancellation token will be triggered through a callback,
            // so that the main loop in the target will only run once.
            var targetTask = this.target.RunAsync(
                this.deviceTelemetryActorObjects,
                threadPosition,
                TELEMETRY_THREAD_COUNT,
                cancellationToken.Token);
            targetTask.CompleteOrTimeout();

            // Assert
            // Verify that RunAsync was called on the expected actors
            var countOfActorCalls = 0;
            foreach (var actor in this.deviceTelemetryActorMocks)
            {
                try
                {
                    actor.Value.Verify(x => x.RunAsync(), Times.Once);
                    countOfActorCalls++;
                }
                catch (MockException)
                {
                }
            }

            // Compare the number of actors that were called to the number of
            // actors that we expect to be called.
            var expectedActorCallCount = chunkSize;
            Assert.Equal(expectedActorCallCount, countOfActorCalls);
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

                // Use a callback to cancel the token so that the main loop of
                // the target will stop after the first iteration.
                mockActor.Setup(x => x.HasWorkToDo()).Returns(true);
                mockActor.Setup(x => x.RunAsync()).Returns(Task.CompletedTask)
                    .Callback(() => { cancellationToken.Cancel(); });

                mockDictionary.TryAdd(deviceName, mockActor);
                objectDictionary.TryAdd(deviceName, mockActor.Object);
            }
        }
    }
}
