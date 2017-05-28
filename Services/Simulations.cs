// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
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
        Simulation Insert(Simulation simulation);
        Simulation Upsert(Simulation simulation);
        Simulation Merge(SimulationPatch patch);
    }

    /// <summary>
    /// Note: Since we don't have a configuration service yet, data is
    /// stored to a JSON file.
    /// </summary>
    public class Simulations : ISimulations
    {
        private const string TempStorageFile = "simulations-storage.json";
        private string tempStoragePath;

        public Simulations()
        {
            this.SetupTempStoragePath();
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

        public Simulation Insert(Simulation simulation)
        {
            var simulations = this.GetList();

            // Only one simulation per deployment
            if (simulations.Count > 0)
            {
                throw new ConflictingResourceException(
                    "There is already a simulation. Only one simulation can be created.");
            }

            simulation.Id = Guid.NewGuid().ToString();

            this.WriteToStorage(simulation);

            return simulation;
        }

        public Simulation Upsert(Simulation simulation)
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

            // TODO: complete validation
            if (string.IsNullOrEmpty(simulation.Id))
            {
                throw new InvalidInputException("Missing ID");
            }

            this.WriteToStorage(simulation);

            return simulation;
        }

        public Simulation Merge(SimulationPatch patch)
        {
            var simulations = this.GetList();

            if (simulations.Count == 0 || simulations[0].Id != patch.Id)
            {
                throw new ResourceNotFoundException("The simulation doesn't exist.");
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

        /// <summary>
        /// While working within an IDE we need to share the data used by the
        /// web service and the data used by the simulation agent, i.e. when
        /// running the web service and the simulation agent from the IDE there
        /// are two "Services/data" folders, one in each entry point. Thus the
        /// simulation agent would not see the temporary storage written by the
        /// web service. By use the user/system temp folder, we make sure the
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

            this.tempStoragePath = (tempFolder + Path.DirectorySeparatorChar + TempStorageFile)
                .Replace(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar,
                    Path.DirectorySeparatorChar.ToString());
            Console.WriteLine("Temporary simulations storage: " + this.tempStoragePath);
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
