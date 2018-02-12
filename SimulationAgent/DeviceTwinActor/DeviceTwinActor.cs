using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTwinActor
{
    public interface IDeviceTwinActor
    {
        Dictionary<string, object> DeviceState { get; }
        IDeviceClient Client { get; }
        DeviceModel.DeviceModelMessage Message { get; }

        void Setup(
            string deviceId,
            DeviceModel deviceModel,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor);

        string Run();
        void HandleEvent(DeviceTelemetryActor.ActorEvents e);
        void Stop();
    }

    public class DeviceTwinActor : IDeviceTwinActor
    {
        public Dictionary<string, object> DeviceState => throw new NotImplementedException();

        public IDeviceClient Client => throw new NotImplementedException();

        public DeviceModel.DeviceModelMessage Message => throw new NotImplementedException();

        public void HandleEvent(DeviceTelemetryActor.ActorEvents e)
        {
            throw new NotImplementedException();
        }

        public string Run()
        {
            throw new NotImplementedException();
        }

        public void Setup(string deviceId, DeviceModel deviceModel, IDeviceStateActor deviceStateActor, IDeviceConnectionActor deviceConnectionActor)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
