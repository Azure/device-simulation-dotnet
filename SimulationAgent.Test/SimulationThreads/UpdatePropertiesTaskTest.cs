// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads;
using Moq;
using Xunit;

namespace SimulationAgent.Test.SimulationThreads
{
    public class UpdatePropertiesTaskTest
    {
        private const int NUM_ACTORS = 9;
        private const int MAX_PENDING_TASKS = 5;

        private readonly Mock<IAppConcurrencyConfig> mockAppConcurrencyConfig;
        private readonly Mock<ILogger> mockLogger;
        private readonly UpdatePropertiesTask target;

        private ConcurrentDictionary<string, Mock<IDevicePropertiesActor>> mockDevicePropertiesActors;
        private ConcurrentDictionary<string, IDevicePropertiesActor> mockDevicePropertiesActorObjects;
        private ConcurrentDictionary<string, Mock<ISimulationManager>> mockSimulationManagers;
        private ConcurrentDictionary<string, ISimulationManager> mockSimulationManagerObjects;

        public UpdatePropertiesTaskTest()
        {
            this.mockDevicePropertiesActors = new ConcurrentDictionary<string, Mock<IDevicePropertiesActor>>();
            this.mockDevicePropertiesActorObjects = new ConcurrentDictionary<string, IDevicePropertiesActor>();
            this.mockSimulationManagers = new ConcurrentDictionary<string, Mock<ISimulationManager>>();
            this.mockSimulationManagerObjects = new ConcurrentDictionary<string, ISimulationManager>();

            this.mockAppConcurrencyConfig = new Mock<IAppConcurrencyConfig>();
            this.mockAppConcurrencyConfig.SetupGet(x => x.MaxPendingTasks).Returns(MAX_PENDING_TASKS);
            this.mockLogger = new Mock<ILogger>();

            this.target = new UpdatePropertiesTask(this.mockAppConcurrencyConfig.Object, this.mockLogger.Object);
        }

        [Fact]
        public void ItCallsRunAsyncOnAllPropertiesActors()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource();

            // Create a list of actors
            this.BuildMockActors(
                this.mockDevicePropertiesActors,
                this.mockDevicePropertiesActorObjects,
                NUM_ACTORS);

            // Build a list of SimulationManagers
            this.BuildMockSimluationManagers(
                this.mockSimulationManagers,
                this.mockSimulationManagerObjects,
                cancellationToken,
                NUM_ACTORS);

            // Act
            // Act on the target. The cancellation token will be cancelled through
            // a callback that will be triggered when each device-connection actor
            // is called.
            var targetTask = this.target.RunAsync(
                this.mockSimulationManagerObjects,
                this.mockDevicePropertiesActorObjects,
                cancellationToken.Token);

            // Assert
            // Verify that each SimulationManager was called at least once
            foreach (var simulationManager in this.mockSimulationManagers)
                simulationManager.Value.Verify(x => x.NewPropertiesLoop(), Times.Once);

            // Verify that each actor was called at least once
            foreach (var actor in this.mockDevicePropertiesActors)
                actor.Value.Verify(x => x.HasWorkToDo(), Times.Once);
        }

        /*
         * Because the target accepts a collection of IDeviceStateActor type, when we pass
         * this dictionary to the target, we will not be able to reference the mocks later
         * (because we're passing a collection of typed objects, not the mocks). This method
         * will generate two collections: one of mocks and one of objects. The second one will
         * be passed to the target, and later, the first one will be used to validate usage.
         */
        private void BuildMockActors(
            ConcurrentDictionary<string, Mock<IDevicePropertiesActor>> mockDictionary,
            ConcurrentDictionary<string, IDevicePropertiesActor> objectDictionary,
            int count)
        {
            mockDictionary.Clear();
            objectDictionary.Clear();

            for (int i = 0; i < count; i++)
            {
                var deviceName = $"device_{i}";
                var mockActor = new Mock<IDevicePropertiesActor>();

                // Have each DeviceConnectionActor report that it has work to do
                mockActor.Setup(x => x.HasWorkToDo()).Returns(true);

                mockDictionary.TryAdd(deviceName, mockActor);
                objectDictionary.TryAdd(deviceName, mockActor.Object);
            }
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
                mockSimulationManager.Setup(x => x.NewPropertiesLoop())
                    .Callback(cancellationToken.Cancel);

                mockSimulationManagers.TryAdd(deviceName, mockSimulationManager);
                mockSimulationManagerObjects.TryAdd(deviceName, mockSimulationManager.Object);
            }
        }
    }
}
