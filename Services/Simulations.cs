// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

// TODO: use real storage
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulations
    {
        IList<Simulation> GetList();
        Simulation Get(string id);
        Simulation Insert(Simulation simulation, string template = "");
        Simulation Upsert(Simulation simulation, string template = "");
        Simulation Merge(SimulationPatch patch);
    }

    /// <summary>
    /// Note: Since we don't have a configuration service yet, data is
    /// stored to a JSON file.
    /// </summary>
    public class Simulations : ISimulations
    {
        private string tempStorageFile = "simulations-storage.json";
        private string tempStoragePath;

        private readonly IDeviceTypes deviceTypes;
        private readonly ILogger log;

        public Simulations(
            IDeviceTypes deviceTypes,
            ILogger logger)
        {
            this.deviceTypes = deviceTypes;
            this.log = logger;

            this.SetupTempStoragePath();
            this.CreateStorageIfMissing();
        }

        // TODO: remove this method and use mocks when we have a storage service
        public void ChangeStorageFile(string filename)
        {
            this.tempStorageFile = filename;
            this.SetupTempStoragePath();
            this.CreateStorageIfMissing();
        }

        public IList<Simulation> GetList()
        {
            this.CreateStorageIfMissing();
            var json = File.ReadAllText(this.tempStoragePath);
            return JsonConvert.DeserializeObject<List<Simulation>>(json);
        }

        public Simulation Get(string id)
        {
            var simulations = this.GetList();
            foreach (var s in simulations)
            {
                if (s.Id == id) return s;
            }

            this.log.Warn("Simulation not found", () => new { id });

            throw new ResourceNotFoundException();
        }

        public Simulation Insert(Simulation simulation, string template = "")
        {
            // TODO: complete validation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() != "default")
            {
                this.log.Warn("Unknown template name", () => new { template });
                throw new InvalidInputException("Unknown template name. Try 'default'.");
            }

            var simulations = this.GetList();

            // Only one simulation per deployment
            if (simulations.Count > 0)
            {
                this.log.Warn("There is already a simulation",
                    () => new { Existing = simulations.First().Id });

                throw new ConflictingResourceException(
                    "There is already a simulation. Only one simulation can be created.");
            }

            // The ID is not empty when using PUT
            if (string.IsNullOrEmpty(simulation.Id))
            {
                simulation.Id = Guid.NewGuid().ToString();
            }

            simulation.Created = DateTimeOffset.UtcNow;
            simulation.Modified = simulation.Created;
            simulation.Version = 1;

            // Create default simulation
            if (!string.IsNullOrEmpty(template) && template.ToLowerInvariant() == "default")
            {
                var types = this.deviceTypes.GetList();
                simulation.DeviceTypes = new List<Simulation.DeviceTypeRef>();
                foreach (var type in types)
                {
                    simulation.DeviceTypes.Add(new Simulation.DeviceTypeRef
                    {
                        Id = type.Id,
                        Count = 2
                    });
                }
            }

            this.WriteToStorage(simulation);

            return simulation;
        }

        public Simulation Upsert(Simulation simulation, string template = "")
        {
            // TODO: complete validation
            if (string.IsNullOrEmpty(simulation.Id))
            {
                this.log.Warn("Missing ID", () => new { simulation });
                throw new InvalidInputException("Missing ID");
            }

            var simulations = this.GetList();
            if (simulations.Count == 0)
            {
                return this.Insert(simulation, template);
            }

            // Note: only one simulation per deployment
            if (simulations[0].Id != simulation.Id)
            {
                this.log.Warn("There is already a simulation",
                    () => new { Existing = simulations[0].Id, Provided = simulation.Id });
                throw new ConflictingResourceException(
                    "There is already a simulation. Only one simulation can be created.");
            }

            if (simulations[0].Etag != simulation.Etag)
            {
                this.log.Warn("Etag mismatch",
                    () => new { Existing = simulations[0].Etag, Provided = simulation.Etag });
                throw new ResourceOutOfDateException(
                    "Etag mismatch: the resource has been updated by another client.");
            }

            simulation.Modified = DateTimeOffset.UtcNow;
            simulation.Version += 1;

            this.WriteToStorage(simulation);

            return simulation;
        }

        public Simulation Merge(SimulationPatch patch)
        {
            var simulations = this.GetList();

            if (simulations.Count == 0 || simulations[0].Id != patch.Id)
            {
                this.log.Warn("The simulation doesn't exist.",
                    () => new { ExistingSimulations = simulations.Count, IdProvided = patch.Id });
                throw new ResourceNotFoundException("The simulation doesn't exist.");
            }

            if (simulations[0].Etag != patch.Etag)
            {
                this.log.Warn("Etag mismatch",
                    () => new { Existing = simulations[0].Etag, Provided = patch.Etag });
                throw new ResourceOutOfDateException(
                    "Etag mismatch: the resource has been updated by another client.");
            }

            var simulation = simulations[0];

            var resourceChanged = false;
            if (patch.Enabled.HasValue && patch.Enabled.Value != simulation.Enabled)
            {
                simulation.Enabled = patch.Enabled.Value;
                resourceChanged = true;
            }

            if (resourceChanged)
            {
                simulation.Modified = DateTimeOffset.UtcNow;
                simulation.Version += 1;
                this.WriteToStorage(simulation);
            }

            return simulation;
        }

        private void WriteToStorage(Simulation simulation)
        {
            simulation.Etag = Etags.NewEtag();
            var data = new List<Simulation> { simulation };
            File.WriteAllText(this.tempStoragePath, JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// While working within an IDE we need to share the data used by the
        /// web service and the data used by the simulation agent, i.e. when
        /// running the web service and the simulation agent from the IDE there
        /// are two "Services/data" folders, one in each entry point. Thus the
        /// simulation agent would not see the temporary storage written by the
        /// web service. By using the user/system temp folder, we make sure the
        /// storage is shared by the two processes when using an IDE.
        /// </summary>
        private void SetupTempStoragePath()
        {
            var tempFolder = Path.GetTempPath();

            // In some cases GetTempPath returns a path under "/var/folders/"
            // in which case we opt for /tmp/. Note: this is temporary until
            // we use a real storage service.
            if (Path.DirectorySeparatorChar == '/' && Directory.Exists("/tmp/"))
            {
                tempFolder = "/tmp/";
            }

            this.tempStoragePath = (tempFolder + Path.DirectorySeparatorChar + this.tempStorageFile)
                .Replace(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar,
                    Path.DirectorySeparatorChar.ToString());

            this.log.Info("Temporary simulations storage: " + this.tempStoragePath, () => { });
        }

        private void CreateStorageIfMissing()
        {
            if (!File.Exists(this.tempStoragePath))
            {
                var data = new List<Simulation>();
                File.WriteAllText(this.tempStoragePath, JsonConvert.SerializeObject(data));
            }
        }
    }
}
