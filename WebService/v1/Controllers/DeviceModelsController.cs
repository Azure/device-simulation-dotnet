// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [ExceptionsFilter]
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

        [HttpGet(Version.PATH + "/[controller]")]
        public async Task<DeviceModelListApiModel> GetAsync()
        {
            return DeviceModelListApiModel.FromServiceModel(await this.deviceModelsService.GetListAsync());
        }

        [HttpGet(Version.PATH + "/[controller]/{id}")]
        public async Task<DeviceModelApiModel> GetAsync(string id)
        {
            return DeviceModelApiModel.FromServiceModel(await this.deviceModelsService.GetAsync(id));
        }

        [HttpPost(Version.PATH + "/[controller]")]
        public async Task<DeviceModelApiModel> PostAsync(
            [FromBody] DeviceModelApiModel deviceModel)
        {
            if (deviceModel == null)
            {
                this.log.Warn("No data provided", () => {});
                throw new BadRequestException("No data provided.");
            }

            deviceModel.ValidateInputRequest(this.log);

            return DeviceModelApiModel.FromServiceModel(await this.deviceModelsService.InsertAsync(deviceModel.ToServiceModel()));
        }

        [HttpPost(Version.PATH + "/[controller]!validate")]
        public ActionResult Validate([FromBody] DeviceModelApiModel deviceModel)
        {
            if (deviceModel == null)
            {
                this.log.Warn("No data provided", () => { });
                throw new BadRequestException("No data provided.");
            }

            var errors = deviceModel.ValidationHelper();

            if (errors.Count > 0)
            {
                return new JsonResult(new DeviceModelApiValidation() {
                    Success = false,
                    Messages = errors
                }) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            return new JsonResult(new DeviceModelApiValidation()) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpPut(Version.PATH + "/[controller]/{id}")]
        public async Task<DeviceModelApiModel> PutAsync(
            [FromBody] DeviceModelApiModel deviceModel,
            string id = "")
        {
            if (deviceModel == null)
            {
                this.log.Warn("No data provided", () => new { });
                throw new BadRequestException("No data provided.");
            }

            deviceModel.ValidateInputRequest(this.log);

            return DeviceModelApiModel.FromServiceModel(
                await this.deviceModelsService.UpsertAsync(deviceModel.ToServiceModel(id)));
        }

        [HttpDelete(Version.PATH + "/[controller]/{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.deviceModelsService.DeleteAsync(id);
        }
    }
}
