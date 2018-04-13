// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Controllers
{
    [Route(Version.PATH + "/[controller]"), ExceptionsFilter]
    public class DeviceModelsController : Controller
    {
        private readonly IDeviceModels deviceModelsService;
        private readonly ILogger log;

        public DeviceModelsController(
            IDeviceModels deviceModelsService,
            ILogger logger)
        {
            this.deviceModelsService = deviceModelsService;
            this.log = logger;
        }

        [HttpGet]
        public async Task<DeviceModelListApiModel> GetAsync()
        {
            return DeviceModelListApiModel.FromServiceModel(await this.deviceModelsService.GetListAsync());
        }

        [HttpGet("{id}")]
        public async Task<DeviceModelApiModel> GetAsync(string id)
        {
            return DeviceModelApiModel.FromServiceModel(await this.deviceModelsService.GetAsync(id));
        }

        [HttpPost]
        public async Task<DeviceModelApiModel> PostAsync(
            [FromBody] DeviceModelApiModel deviceModel)
        {
            deviceModel?.ValidateInputRequest(this.log);

            if (deviceModel == null)
            {
                this.log.Warn("No data or invalid data provided", () => new { deviceModel });
                throw new BadRequestException("No data or invalid data provided.");
            }

            return DeviceModelApiModel.FromServiceModel(
                await this.deviceModelsService.InsertAsync(deviceModel.ToServiceModel()));
        }

        [HttpPut("{id}")]
        public async Task<DeviceModelApiModel> PutAsync(
            [FromBody] DeviceModelApiModel deviceModel,
            string id = "")
        {
            deviceModel?.ValidateInputRequest(this.log);

            if (deviceModel == null)
            {
                this.log.Warn("No data or invalid data provided", () => new { deviceModel });
                throw new BadRequestException("No data or invalid data provided.");
            }

            return DeviceModelApiModel.FromServiceModel(
                await this.deviceModelsService.UpsertAsync(deviceModel.ToServiceModel(id)));
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.deviceModelsService.DeleteAsync(id);
        }
    }
}