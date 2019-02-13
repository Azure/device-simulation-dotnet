// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [ExceptionsFilter]
    public class DeviceModelScriptsController : Controller
    {
        private const string TEXT_JAVASCRIPT = "text/javascript";
        private readonly ILogger log;
        private readonly IDeviceModelScripts simulationScriptService;
        private readonly IJavascriptInterpreter javascriptInterpreter;

        public DeviceModelScriptsController(
            IDeviceModelScripts simulationScriptService,
            IJavascriptInterpreter javascriptInterpreter,
            ILogger logger)
        {
            this.simulationScriptService = simulationScriptService;
            this.javascriptInterpreter = javascriptInterpreter;
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
        public ActionResult Validate(IFormFile file)
        {
            if (file == null)
            {
                this.log.Warn("No data provided");
                throw new BadRequestException("No data provided.");
            }

            if (file.ContentType != TEXT_JAVASCRIPT && !file.FileName.EndsWith(".js"))
            {
                this.log.Warn("Wrong content type provided", () => new { file.ContentType });
                throw new BadRequestException("Wrong content type provided.");
            }

            try
            {
                var content = this.javascriptInterpreter.Validate(file.OpenReadStream());
            }
            catch (Exception e)
            {
                return new JsonResult(new ValidationApiModel
                    {
                        IsValid = false,
                        Messages = new List<string>
                        {
                            e.Message
                        }
                    })
                    { StatusCode = (int) HttpStatusCode.BadRequest };
            }

            return new JsonResult(new ValidationApiModel()) { StatusCode = (int) HttpStatusCode.OK };
        }

        [HttpPost(Version.PATH + "/[controller]")]
        public async Task<DeviceModelScriptApiModel> PostAsync(IFormFile file)
        {
            if (file == null)
            {
                this.log.Warn("No data provided");
                throw new BadRequestException("No data provided.");
            }

            if (file.ContentType != TEXT_JAVASCRIPT && !file.FileName.EndsWith(".js"))
            {
                this.log.Warn("Wrong content type provided", () => new { file.ContentType });
                throw new BadRequestException("Wrong content type provided.");
            }

            var deviceModelScript = new DataFile();

            try
            {
                var content = this.javascriptInterpreter.Validate(file.OpenReadStream());
                deviceModelScript.Content = content;
                deviceModelScript.Name = file.FileName;
                deviceModelScript.Type = ScriptInterpreter.JAVASCRIPT_SCRIPT;
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

            var simulationScript = new DataFile
            {
                ETag = eTag,
                Id = id
            };

            try
            {
                var reader = new StreamReader(file.OpenReadStream());
                simulationScript.Content = reader.ReadToEnd();
                simulationScript.Name = file.FileName;
                simulationScript.Type = ScriptInterpreter.JAVASCRIPT_SCRIPT;
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
