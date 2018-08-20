// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Jint.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Filters;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelScriptApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [ExceptionsFilter]
    public class DeviceModelScriptsController : Controller
    {
        private readonly string ApplicationJavascript = "application/javascript";
        private readonly ILogger log;
        private readonly IDeviceModelScripts simulationScriptService;

        public DeviceModelScriptsController(
            IDeviceModelScripts simulationScriptService,
            ILogger logger)
        {
            this.simulationScriptService = simulationScriptService;
            this.log = logger;
        }

        [HttpGet(Version.PATH + "/[controller]")]
        public async Task<DeviceModelScriptListModel> GetAsync()
        {
            return DeviceModelScriptListModel.FromServiceModel(await this.simulationScriptService.GetListAsync());
        }

        [HttpGet(Version.PATH + "/[controller]/{id}")]
        public async Task<DeviceModelScriptApiModel> GetAsync(string id)
        {
            return DeviceModelScriptApiModel.FromServiceModel(await this.simulationScriptService.GetAsync(id));
        }

        [HttpPost(Version.PATH + "/[controller]!validate")]
        public async Task<ActionResult> Validate(IFormFile file)
        {
            if (file == null)
            {
                this.log.Warn("No data provided");
                throw new BadRequestException("No data provided.");
            }

            if (file.ContentType != this.ApplicationJavascript)
            {
                this.log.Warn("Wrong content type provided", () => new { file.ContentType });
                throw new BadRequestException("Wrong content type provided.");
            }

            try
            {
                var parser = new JavaScriptParser();
                var reader = new StreamReader(file.OpenReadStream());
                var rawScript = reader.ReadToEnd();
                parser.Parse(rawScript);
            }
            catch (Exception e)
            {
                var result = new JsonResult(new ValidationApiModel
                    {
                        IsValid = false,
                        Messages = new List<string>
                        {
                            e.Message
                        }
                    })
                    { StatusCode = (int) HttpStatusCode.BadRequest };
                return await Task.FromResult(result);
            }

            return await Task.FromResult(new JsonResult(new ValidationApiModel()) { StatusCode = (int) HttpStatusCode.OK });
        }

        [HttpPost(Version.PATH + "/[controller]")]
        public async Task<DeviceModelScriptApiModel> PostAsync(IFormFile file)
        {
            if (file == null)
            {
                this.log.Warn("No data provided");
                throw new BadRequestException("No data provided.");
            }

            if (file.ContentType != this.ApplicationJavascript)
            {
                this.log.Warn("Wrong content type provided", () => new { file.ContentType });
                throw new BadRequestException("Wrong content type provided.");
            }

            var deviceModelScript = new DeviceModelScript();

            try
            {
                var reader = new StreamReader(file.OpenReadStream());
                deviceModelScript.Content = reader.ReadToEnd();
                deviceModelScript.Name = file.FileName;
            }
            catch (Exception e)
            {
                throw new BadRequestException(e.Message);
            }

            return DeviceModelScriptApiModel.FromServiceModel(await this.simulationScriptService.InsertAsync(deviceModelScript));
        }

        [HttpPut(Version.PATH + "/[controller]/{id}")]
        public async Task<DeviceModelScriptApiModel> PutAsync(
            IFormFile file,
            string eTag,
            string id)
        {
            if (file == null)
            {
                this.log.Warn("No data provided");
                throw new BadRequestException("No data provided.");
            }

            if (string.IsNullOrEmpty(id))
            {
                this.log.Warn("No id provided");
                throw new BadRequestException("No id provided.");
            }

            if (string.IsNullOrEmpty(eTag))
            {
                this.log.Warn("No ETag provided");
                throw new BadRequestException("No ETag provided.");
            }

            var simulationScript = new DeviceModelScript
            {
                ETag = eTag,
                Id = id
            };

            try
            {
                var reader = new StreamReader(file.OpenReadStream());
                simulationScript.Content = reader.ReadToEnd();
                simulationScript.Name = file.FileName;
            }
            catch (Exception e)
            {
                throw new BadRequestException(e.Message);
            }

            return DeviceModelScriptApiModel.FromServiceModel(await this.simulationScriptService.UpsertAsync(simulationScript));
        }

        [HttpDelete(Version.PATH + "/[controller]/{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.simulationScriptService.DeleteAsync(id);
        }
    }
}
