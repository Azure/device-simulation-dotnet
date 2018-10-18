// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class SimulationDeviceModelRefTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsAListOfDeviceModelRefFromServiceModel()
        {
            // Arrange
            var deviceModelRefs = this.GetDeviceModelRefs();

            // Act
            var result = SimulationDeviceModelRef.FromServiceModel(deviceModelRefs);

            // Assert
            Assert.IsType<SimulationDeviceModelRef>(result.FirstOrDefault());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsDeviceModelRefFromApiModel()
        {
            // Arrange
            var deviceModelRef = this.GetDeviceModelRef();

            // Act
            var result = deviceModelRef.ToServiceModel();

            // Assert
            Assert.IsType<Simulation.DeviceModelRef>(result);
        }

        private SimulationDeviceModelRef GetDeviceModelRef()
        {
            var deviceModelRef = new SimulationDeviceModelRef();

            return deviceModelRef;
        }

        private IEnumerable<Simulation.DeviceModelRef> GetDeviceModelRefs()
        {
            var deviceModelRef = new List<Simulation.DeviceModelRef>()
            {
                new Simulation.DeviceModelRef()
            };

            return deviceModelRef;
        }
    }
}
