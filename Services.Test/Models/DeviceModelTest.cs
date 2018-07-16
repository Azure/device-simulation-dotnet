// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Models
{
    public class DeviceModelTest
    {
        private readonly ITestOutputHelper log;

        public DeviceModelTest(ITestOutputHelper log)
        {
            this.log = log;
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanBeSerializedAndDeserialized()
        {
            // Arrange
            var x = new DeviceModel
            {
                ETag = RndText(),
                Id = RndText(),
                Version = RndText(),
                Name = RndText(),
                Description = RndText(),
                Type = RndEnum<DeviceModel.DeviceModelType>(),
                Protocol = RndEnum<IoTHubProtocol>(),
                Simulation = new DeviceModel.StateSimulation
                {
                    InitialState = new Dictionary<string, object>
                    {
                        { RndText(), RndInt() },
                        { RndText(), true },
                        { RndText(), RndText() }
                    },
                    Interval = TimeSpan.FromSeconds(Guid.NewGuid().ToByteArray().First()),
                    Scripts = new List<Script>
                    {
                        new Script
                        {
                            Type = RndText(),
                            Path = RndText(),
                            Params = new Dictionary<string, string>
                            {
                                { RndText(), RndText() },
                                { RndText(), RndText() }
                            }
                        }
                    }
                },
                Properties = new Dictionary<string, object>
                {
                    { RndText(), RndInt() },
                    { RndText(), false },
                    { RndText(), RndText() }
                },
                Telemetry = new List<DeviceModel.DeviceModelMessage>
                {
                    new DeviceModel.DeviceModelMessage
                    {
                        Interval = TimeSpan.FromSeconds(RndInt()),
                        MessageTemplate = RndText(),
                        MessageSchema = new DeviceModel.DeviceModelMessageSchema
                        {
                            Name = RndText(),
                            Format = RndEnum<DeviceModel.DeviceModelMessageSchemaFormat>(),
                            Fields = new Dictionary<string, DeviceModel.DeviceModelMessageSchemaType>
                            {
                                { RndText(), RndEnum<DeviceModel.DeviceModelMessageSchemaType>() },
                                { RndText(), RndEnum<DeviceModel.DeviceModelMessageSchemaType>() },
                                { RndText(), RndEnum<DeviceModel.DeviceModelMessageSchemaType>() }
                            }
                        }
                    }
                },
                CloudToDeviceMethods = new Dictionary<string, Script>
                {
                    {
                        RndText(), new Script
                        {
                            Type = RndText(),
                            Path = RndText(),
                            Params = null
                        }
                    }
                },
                Created = DateTimeOffset.UtcNow,
                Modified = DateTimeOffset.UtcNow
            };

            // Act
            var json = JsonConvert.SerializeObject(x);
            this.log.WriteLine(json);
            var y = JsonConvert.DeserializeObject<DeviceModel>(json);

            // Assert
            Assert.Empty(y.ETag);
            Assert.Equal(x.Id, y.Id);
            Assert.Equal(x.Version, y.Version);
            Assert.Equal(x.Name, y.Name);
            Assert.Equal(x.Description, y.Description);
            Assert.Equal(x.Type, y.Type);
            Assert.Equal(x.Protocol, y.Protocol);

            // Simulation initial state
            Assert.Equal(x.Simulation.InitialState.Count, y.Simulation.InitialState.Count);
            Assert.Equal(x.Simulation.InitialState.ElementAt(0).Key, y.Simulation.InitialState.ElementAt(0).Key);
            Assert.Equal(x.Simulation.InitialState.ElementAt(1).Key, y.Simulation.InitialState.ElementAt(1).Key);
            Assert.Equal(x.Simulation.InitialState.ElementAt(2).Key, y.Simulation.InitialState.ElementAt(2).Key);
            Assert.Equal(x.Simulation.InitialState.ElementAt(0).Value, y.Simulation.InitialState.ElementAt(0).Value);
            Assert.Equal(x.Simulation.InitialState.ElementAt(1).Value, y.Simulation.InitialState.ElementAt(1).Value);
            Assert.Equal(x.Simulation.InitialState.ElementAt(2).Value, y.Simulation.InitialState.ElementAt(2).Value);

            Assert.Equal(x.Simulation.Interval, y.Simulation.Interval);
            Assert.Equal(x.Simulation.Scripts.Count, y.Simulation.Scripts.Count);
            Assert.Equal(x.Simulation.Scripts[0].Type, y.Simulation.Scripts[0].Type);
            Assert.Equal(x.Simulation.Scripts[0].Path, y.Simulation.Scripts[0].Path);

            // Simulation scripts params
            Assert.Equal(
                ((Dictionary<string, string>) x.Simulation.Scripts[0].Params).Count,
                ((JObject) y.Simulation.Scripts[0].Params).Count);
            // 1st
            var index = 0;
            var item = ((Dictionary<string, string>) x.Simulation.Scripts[0].Params).ElementAt(index);
            Assert.Equal(item.Value, ((JObject) y.Simulation.Scripts[0].Params)[item.Key]);
            // 2nd
            index++;
            item = ((Dictionary<string, string>) x.Simulation.Scripts[0].Params).ElementAt(index);
            Assert.Equal(item.Value, ((JObject) y.Simulation.Scripts[0].Params)[item.Key]);

            // Device model properties
            Assert.Equal(x.Properties.Count, y.Properties.Count);
            Assert.Equal(x.Properties.ElementAt(0).Key, y.Properties.ElementAt(0).Key);
            Assert.Equal(x.Properties.ElementAt(0).Value, y.Properties.ElementAt(0).Value);
            Assert.Equal(x.Properties.ElementAt(1).Key, y.Properties.ElementAt(1).Key);
            Assert.Equal(x.Properties.ElementAt(1).Value, y.Properties.ElementAt(1).Value);
            Assert.Equal(x.Properties.ElementAt(2).Key, y.Properties.ElementAt(2).Key);
            Assert.Equal(x.Properties.ElementAt(2).Value, y.Properties.ElementAt(2).Value);
            
            // Telemetry messages
            Assert.Equal(x.Telemetry.Count, y.Telemetry.Count);
            Assert.Equal(x.Telemetry[0].Interval, y.Telemetry[0].Interval);
            Assert.Equal(x.Telemetry[0].MessageTemplate, y.Telemetry[0].MessageTemplate);
            Assert.Equal(x.Telemetry[0].MessageSchema.Name, y.Telemetry[0].MessageSchema.Name);
            Assert.Equal(x.Telemetry[0].MessageSchema.Format, y.Telemetry[0].MessageSchema.Format);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.Count, y.Telemetry[0].MessageSchema.Fields.Count);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.ElementAt(0).Key, y.Telemetry[0].MessageSchema.Fields.ElementAt(0).Key);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.ElementAt(0).Value, y.Telemetry[0].MessageSchema.Fields.ElementAt(0).Value);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.ElementAt(1).Key, y.Telemetry[0].MessageSchema.Fields.ElementAt(1).Key);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.ElementAt(1).Value, y.Telemetry[0].MessageSchema.Fields.ElementAt(1).Value);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.ElementAt(2).Key, y.Telemetry[0].MessageSchema.Fields.ElementAt(2).Key);
            Assert.Equal(x.Telemetry[0].MessageSchema.Fields.ElementAt(2).Value, y.Telemetry[0].MessageSchema.Fields.ElementAt(2).Value);
            
            Assert.Equal(x.CloudToDeviceMethods.Count, y.CloudToDeviceMethods.Count);
            Assert.Equal(x.CloudToDeviceMethods.ElementAt(0).Key, y.CloudToDeviceMethods.ElementAt(0).Key);
            Assert.Equal(x.CloudToDeviceMethods.ElementAt(0).Value.Type, y.CloudToDeviceMethods.ElementAt(0).Value.Type);
            Assert.Equal(x.CloudToDeviceMethods.ElementAt(0).Value.Path, y.CloudToDeviceMethods.ElementAt(0).Value.Path);
            Assert.Equal(x.CloudToDeviceMethods.ElementAt(0).Value.Params, y.CloudToDeviceMethods.ElementAt(0).Value.Params);
            
            Assert.Equal(x.Created, y.Created);
            Assert.Equal(x.Modified, y.Modified);
        }

        private static T RndEnum<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T) v.GetValue(new Random().Next(v.Length));
        }

        private static string RndText()
        {
            return Guid.NewGuid().ToString();
        }

        private static long RndInt()
        {
            return Guid.NewGuid().ToByteArray().First();
        }
    }
}
