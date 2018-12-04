// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public interface IEngines
    {
        IEngine Build(Config config);
    }

    public class Engines : IEngines
    {
        private readonly IFactory factory;

        public Engines(IFactory factory)
        {
            this.factory = factory;
        }

        public IEngine Build(Config config)
        {
            IEngine engine;

            switch (config.StorageType)
            {
                case Type.CosmosDbSql:
                    engine = this.factory.Resolve<CosmosDbSql.Engine>();
                    engine.Init(config);
                    return engine;

                case Type.TableStorage:
                    engine = this.factory.Resolve<TableStorage.Engine>();
                    engine.Init(config);
                    return engine;
            }

            throw new ArgumentOutOfRangeException("Unknown storage engine: " + config.StorageType);
        }
    }
}
