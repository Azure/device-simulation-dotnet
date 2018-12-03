// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public interface IEngines
    {
        IEngine Build(Config config);
    }

    public class Engines : IEngines
    {
        private readonly IFactory factory;
        private readonly IInstance instance;

        public Engines(IFactory factory, IInstance instance)
        {
            this.factory = factory;
            this.instance = instance;
        }

        public IEngine Build(Config config)
        {
            this.instance.InitOnce();

            IEngine engine;

            switch (config.StorageType)
            {
                case Type.CosmosDbSql:
                    engine = this.factory.Resolve<Engine>();
                    engine.Init(config);
                    this.instance.InitComplete();
                    return engine;

                case Type.TableStorage:
                    engine = this.factory.Resolve<TableStorage.Engine>();
                    engine.Init(config);
                    this.instance.InitComplete();
                    return engine;
            }

            throw new ArgumentOutOfRangeException("Unknown storage engine: " + config.StorageType);
        }
    }
}
