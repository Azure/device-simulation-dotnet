// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Moq;
using Newtonsoft.Json;
using SimulationAgent.Test.helpers;
using Xunit;

namespace SimulationAgent.Test
{
    public class AgentTest
    {
        private readonly Agent target;
        private readonly Mock<IAppConcurrencyConfig> appConcurrencyConfig;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<IFactory> factory;
        private readonly Mock<ILogger> log;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<IThreadWrapper> thread;
        private readonly Mock<IFile> file;

        public AgentTest()
        {
            this.appConcurrencyConfig = new Mock<IAppConcurrencyConfig>();
            this.simulations = new Mock<ISimulations>();
            this.factory = new Mock<IFactory>();
            this.log = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.thread = new Mock<IThreadWrapper>();
            this.file = new Mock<IFile>();

            this.target = new Agent(
                this.appConcurrencyConfig.Object,
                this.simulations.Object,
                this.factory.Object,
                this.log.Object,
                this.diagnosticsLogger.Object,
                this.file.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesSampleSimulations()
        {
            // Arrange
            this.ThereIsNoSimulationInStorage();
            this.ThereIsASampleSimulationTemplate();

            // Act
            this.target.SeedAsync(It.IsAny<string>()).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.UpsertAsync(It.IsAny<Simulation>()), Times.AtLeastOnce);
        }

        private void ThereIsNoSimulationInStorage()
        {            
            this.simulations
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new ResourceNotFoundException());
        }

        private void ThereIsASampleSimulationTemplate()
        {
            var simulationList = new List<Simulation>()
            {
                new Simulation()
            };
            string TEMPLATE_FILE = JsonConvert.SerializeObject(simulationList);

            this.file.Setup(x => x.Exists(It.IsAny<string>()))
                .Returns(true);
            this.file.Setup(x => x.ReadAllText(It.IsAny<string>()))
                .Returns(TEMPLATE_FILE);
        }
    }
}
