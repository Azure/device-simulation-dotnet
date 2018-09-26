// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.SimulationApiModel
{
    public class MetricsApiModelTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReturnsMetricsRequestsModelFromApiModel()
        {
            // Arrange
            var requests = new List<MetricsRequestApiModel>
            {
                new MetricsRequestApiModel()
            };

            var apiModel = new MetricsRequestsApiModel
            {
                Requests = requests
            };

            // Act
            var result = apiModel.ToServiceModel();

            // Assert
            Assert.IsType<MetricsRequestListModel>(result);
            Assert.Equal(requests.Count, result.Requests.Count);
        }
    }
}
