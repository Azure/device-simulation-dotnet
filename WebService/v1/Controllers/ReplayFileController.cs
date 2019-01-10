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
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.ReplayFileApiModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Controllers
{
    [ExceptionsFilter]
    public class ReplayFileController : Controller
    {
        private const string TEXT_CSV = "text/csv";
        private const string TYPE_CSV = "csv";
        private readonly ILogger log;
        private readonly IReplayFileService replayFileService;

        public ReplayFileController(
            IReplayFileService replayFileService,
            ILogger logger)
        {
            this.replayFileService = replayFileService;
            this.log = logger;
        }

        [HttpGet(Version.PATH + "/[controller]/{id}")]
        public async Task<ReplayFileApiModel> GetAsync(string id)
        {
            return ReplayFileApiModel.FromServiceModel(await this.replayFileService.GetAsync(id));
        }

        [HttpPost(Version.PATH + "/[controller]!validate")]
        public ActionResult Validate(IFormFile file)
        {
            try
            {
                var content = this.replayFileService.ValidateFile(file.OpenReadStream());
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
        public async Task<ReplayFileApiModel> PostAsync(IFormFile file)
        {
            var replayFile = new DataFile();

            try
            {
                var content = this.replayFileService.ValidateFile(file.OpenReadStream());
                replayFile.Content = content;
                replayFile.Name = file.FileName;
            }
            catch (Exception e)
            {
                throw new BadRequestException(e.Message);
            }

            return ReplayFileApiModel.FromServiceModel(await this.replayFileService.InsertAsync(replayFile));
        }

        [HttpDelete(Version.PATH + "/[controller]/{id}")]
        public async Task DeleteAsync(string id)
        {
            await this.replayFileService.DeleteAsync(id);
        }

        private void ValidateInput(IFormFile file)
        {
            if (file == null)
            {
                this.log.Warn("No replay data provided");
                throw new BadRequestException("No replay data provided.");
            }

            if (file.ContentType != TEXT_CSV && !file.FileName.EndsWith(".csv"))
            {
                this.log.Warn("Wrong content type provided. Expected csv file format.", () => new { file.ContentType });
                throw new BadRequestException("Wrong content type provided. Expected csv file format.");
            }
        }
    }
}
