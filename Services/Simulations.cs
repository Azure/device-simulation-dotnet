// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

// TODO: use real storage
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface ISimulations
    {
        IList<Simulation> GetList();
        Simulation Get(string id);
        Simulation Create(Simulation simulation);
        Simulation Merge(SimulationPatch patch);
    }

    /// <summary>
    /// Note: Since we don't have a configuration service yet, data is
    /// stored to a JSON file.
    /// </summary>
    public class Simulations : ISimulations
    {
        private const string TempStorageFile = "tempstorage.json";
        private readonly string tempStoragePath;

        public Simulations(IServicesConfig config)
        {
            this.tempStoragePath = config.StorageFolder +
                                  Path.DirectorySeparatorChar +
                                  TempStorageFile;
            this.CreateStorageIfMissing();
        }

        public IList<Simulation> GetList()
        {
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

            throw new ResourceNotFoundException();
        }

        public Simulation Create(Simulation simulation)
        {
            var simulations = this.GetList();

            // Only one simulation per deployment
            if (simulations.Count > 0)
            {
                if (simulations[0].Id != simulation.Id)
                {
                    throw new ConflictingResourceException(
                        "There is already a simulation. Only one simulation can be created.");
                }

                if (simulations[0].Etag != simulation.Etag)
                {
                    throw new ResourceOutOfDateException(
                        "Etag mismatch: the resource has been updated by another client.");
                }
            }

            if (string.IsNullOrEmpty(simulation.Id))
            {
                simulation.Id = Guid.NewGuid().ToString();
            }

            this.WriteToStorage(simulation);

            return simulation;
        }

        public Simulation Merge(SimulationPatch patch)
        {
            var simulations = this.GetList();

            if (simulations.Count == 0 || simulations[0].Id != patch.Id)
            {
                throw new ResourceNotFoundException();
            }

            if (simulations[0].Etag != patch.Etag)
            {
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
