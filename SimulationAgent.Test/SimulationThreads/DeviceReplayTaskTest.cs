﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceReplay;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads;
using Moq;
using Xunit;

namespace SimulationAgent.Test.SimulationThreads
{
    public class DeviceReplayTaskTest
    {
        private const int NUM_ACTORS = 9;
        private const int MAX_PENDING_TASKS = 5;

        private readonly Mock<IAppConcurrencyConfig> mockAppConcurrencyConfig;
        private readonly Mock<ILogger> mockLogger;
        private readonly DeviceReplayTask target;
        private readonly ConcurrentDictionary<string, IDeviceReplayActor> mockDeviceReplayActorObjects;
        private readonly ConcurrentDictionary<string, Mock<ISimulationManager>> mockSimulationManagers;
        private readonly ConcurrentDictionary<string, ISimulationManager> mockSimulationManagerObjects;

        public DeviceReplayTaskTest()
        {
            this.mockDeviceReplayActorObjects = new ConcurrentDictionary<string, IDeviceReplayActor>();
            this.mockSimulationManagers = new ConcurrentDictionary<string, Mock<ISimulationManager>>();
            this.mockSimulationManagerObjects = new ConcurrentDictionary<string, ISimulationManager>();

            this.mockAppConcurrencyConfig = new Mock<IAppConcurrencyConfig>();
            this.mockAppConcurrencyConfig.SetupGet(x => x.MaxPendingTasks).Returns(MAX_PENDING_TASKS);
            this.mockLogger = new Mock<ILogger>();

            this.target = new DeviceReplayTask(this.mockAppConcurrencyConfig.Object, this.mockLogger.Object);
        }

        [Fact]
        public void ItCallsRunAsyncOnAllReplayActors()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource();

            // Build a list of SimulationManagers
            // TODO: Create replay actors
            this.BuildMockSimluationManagers(
                this.mockSimulationManagers,
                this.mockSimulationManagerObjects,
                cancellationToken,
                NUM_ACTORS);

            // Act
            // Act on the target. The cancellation token will be cancelled through
            // a callback that will be triggered when each device-replay actor
            // is called.
            var targetTask = this.target.RunAsync(
                this.mockSimulationManagerObjects,
                this.mockDeviceReplayActorObjects,
                cancellationToken.Token);

            // Assert
            // Verify that each SimulationManager was called at least once
            foreach (var simulationManager in this.mockSimulationManagers)
                simulationManager.Value.Verify(x => x.NewConnectionLoop(), Times.Once);
        }

        /*
         * Creating two collections: one for the mocks, and another to store the
         * mock objects. If we only created one collection and populated it with
         * the mock objects, we wouldn't have a reference to the backing mock for
         * each.
         */
        private void BuildMockSimluationManagers(
            ConcurrentDictionary<string, Mock<ISimulationManager>> mockSimulationManagers,
            ConcurrentDictionary<string, ISimulationManager> mockSimulationManagerObjects,
            CancellationTokenSource cancellationToken,
            int count)
        {
            mockSimulationManagers.Clear();
            mockSimulationManagerObjects.Clear();

            for (int i = 0; i < count; i++)
            {
                var deviceName = $"simulation_{i}";
                var mockSimulationManager = new Mock<ISimulationManager>();

                // We only want the main loop in the target to run once, so here we'll
                // trigger a callback which will cancel the cancellation token that
                // the main loop uses.
                mockSimulationManager.Setup(x => x.NewConnectionLoop())
                    .Callback(() => cancellationToken.Cancel());

                mockSimulationManagers.TryAdd(deviceName, mockSimulationManager);
                mockSimulationManagerObjects.TryAdd(deviceName, mockSimulationManager.Object);
            }
        }
    }
}
