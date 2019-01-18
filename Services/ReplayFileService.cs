// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IReplayFileService
    {
        /// <summary>
        /// Get a replay file.
        /// </summary>
        Task<DataFile> GetAsync(string id);

        /// <summary>
        /// Create a replay file.
        /// </summary>
        Task<DataFile> InsertAsync(DataFile replayFile);

        /// <summary>
        /// Delete a replay file.
        /// </summary>
        Task DeleteAsync(string id);

        /// <summary>
        /// Validate replay file.
        /// </summary>
        string ValidateFile(Stream stream);
    }

    public class ReplayFileService : IReplayFileService
    {
        private const int NUM_CSV_COLS = 3;
        private readonly IEngine replayFilesStorage;
        private readonly ILogger log;

        public ReplayFileService(
            IServicesConfig config,
            IEngines engines,
            ILogger logger)
        {
            this.replayFilesStorage = engines.Build(config.ReplayFilesStorage);
            this.log = logger;
        }

        /// <summary>
        /// Delete a device model script.
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            try
            {
                await this.replayFilesStorage.DeleteAsync(id);
            }
            catch (Exception e)
            {
                this.log.Error("Something went wrong while deleting the replay file.", () => new { id, e });
                throw new ExternalDependencyException("Failed to delete the replay file", e);
            }
        }

        /// <summary>
        /// Get a device model script.
        /// </summary>
        public async Task<DataFile> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                this.log.Error("Simulation script id cannot be empty!");
                throw new InvalidInputException("Simulation script id cannot be empty! ");
            }

            IDataRecord item;
            try
            {
                item = await this.replayFilesStorage.GetAsync(id);
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to load replay file from storage", () => new { id, e });
                throw new ExternalDependencyException("Unable to load device model script from storage", e);
            }

            try
            {
                var deviceModelScript = JsonConvert.DeserializeObject<DataFile>(item.GetData());
                deviceModelScript.ETag = item.GetETag();
                return deviceModelScript;
            }
            catch (Exception e)
            {
                this.log.Error("Unable to parse device model script loaded from storage", () => new { id, e });
                throw new ExternalDependencyException("Unable to parse device model script loaded from storage", e);
            }
        }

        /// <summary>
        /// Create a device model script.
        /// </summary>
        public async Task<DataFile> InsertAsync(DataFile replayFile)
        {
            replayFile.Created = DateTimeOffset.UtcNow;
            replayFile.Modified = replayFile.Created;

            if (string.IsNullOrEmpty(replayFile.Id))
            {
                replayFile.Id = Guid.NewGuid().ToString();
            }

            this.log.Debug("Creating a new replay file.", () => new { replayFile });

            try
            {
                IDataRecord record = this.replayFilesStorage.BuildRecord(replayFile.Id,
                    JsonConvert.SerializeObject(replayFile));

                var result = await this.replayFilesStorage.CreateAsync(record);

                replayFile.ETag = result.GetETag();
            }
            catch (Exception e)
            {
                this.log.Error("Failed to insert new replay file into storage",
                    () => new { replayFile, e });
                throw new ExternalDependencyException(
                    "Failed to insert new replay file into storage", e);
            }

            return replayFile;
        }

        /// <summary>
        /// Validate replay file
        /// </summary>
        public string ValidateFile(Stream stream)
        {
            var reader = new StreamReader(stream);
            var file = reader.ReadToEnd();
            
            while (!reader.EndOfStream)
            {
                try
                {
                    string line = reader.ReadLine();
                    string[] fields = line.Split(',');
                    if (fields.Length < NUM_CSV_COLS)
                    {
                        this.log.Error("Replay file has invalid csv format");
                        throw new InvalidInputException("Replay file has invalid csv format");
                    }
                }
                catch (Exception ex)
                {
                    this.log.Error("Error parsing replay file", () => new { ex });
                    throw new InvalidInputException("Error parsing replay file", ex);
                }
            }
 
            return file;
        }
    }
}
