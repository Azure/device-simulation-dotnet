// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using WebService.Test.helpers;
using Xunit;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace WebService.Test.v1.Models
{
    public class DeviceModelSimulationTest
    {
        private readonly Mock<ILogger> logger;

        public DeviceModelSimulationTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelSimulationFromServiceModel()
        {
            // Arrange
            var stateSimulation = this.GetDeviceModelStateSimulation();

            // Act
            var result = DeviceModelSimulation.FromServiceModel(stateSimulation);

            // Assert
            Assert.IsType<DeviceModelSimulation>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsStateSimulationFromDeviceModelSimulation()
        {
            // Arrange
            var deviceModelSimulation = this.GetDeviceModelSimulation();

            // Act
            var result = DeviceModelSimulation.ToServiceModel(deviceModelSimulation);

            // Assert
            Assert.IsType<StateSimulation>(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void NoExceptionThrownForValidDeviceModelSimulation()
        {
            // Arrange
            var deviceModelSimulation = this.GetDeviceModelSimulation();

            // Act
            var ex = Record.Exception(() => deviceModelSimulation.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.Null(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidInterval()
        {
            // Arrange
            DeviceModelSimulation InvalidInterval(DeviceModelSimulation model)
            {
                model.Interval = "";
                return model;
            }

            var deviceModelSimulation = this.GetInvalidDeviceModelSimulation(InvalidInterval);

            // Act
            var ex = Record.Exception(() => deviceModelSimulation.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsBadRequestExceptionForInvalidScript()
        {
            // Arrange
            DeviceModelSimulation InvalidScript(DeviceModelSimulation model)
            {
                model.Scripts = new List<DeviceModelSimulationScript>();
                return model;
            }

            var deviceModelSimulation = this.GetInvalidDeviceModelSimulation(InvalidScript);

            // Act
            var ex = Record.Exception(() => deviceModelSimulation.ValidateInputRequest(this.logger.Object));

            // Assert
            Assert.IsType<BadRequestException>(ex);
        }

        private DeviceModelSimulation GetInvalidDeviceModelSimulation(Func<DeviceModelSimulation, DeviceModelSimulation> func)
        {
            var model = this.GetDeviceModelSimulation();
            return func(model);
        }

        private DeviceModelSimulation GetDeviceModelSimulation()
        {
            var deviceModelSimulation = new DeviceModelSimulation
            {
                Interval = "00:00:10",
                Scripts = new List<DeviceModelSimulationScript>
                {
                    new DeviceModelSimulationScript
                    {
                        Type = ScriptInterpreter.JAVASCRIPT_SCRIPT,
                        Path = "script"
                    }
                }
            };

            return deviceModelSimulation;
        }

        private StateSimulation GetDeviceModelStateSimulation()
        {
            var deviceModelSimulation = new StateSimulation
            {
                Interval= TimeSpan.FromSeconds(10),
                Scripts = new List<Script>
                {
                    new Script
                    {
                        Type = ScriptInterpreter.JAVASCRIPT_SCRIPT,
                        Path = "scripts"
                    }
                }
            };

            return deviceModelSimulation;
        }
    }
}
