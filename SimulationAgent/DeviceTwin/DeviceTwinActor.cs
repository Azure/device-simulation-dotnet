using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTwin;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTwinActor
{
    public interface IDeviceTwinActor
    {
        Dictionary<string, object> DeviceState { get; }
        IDeviceClient Client { get; }
        Services.Models.DeviceTwin DeviceTwin { get; }

        void Setup(
            string deviceId,
            Services.Models.DeviceTwin deviceTwin,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor);

        string Run();
        void HandleEvent(DeviceTwinActor.ActorEvents e);
        void Stop();
    }

    public class DeviceTwinActor : IDeviceTwinActor
    {
        private enum ActorStatus
        {
            None,
            ReadyToStart,
            ReadyToSend,
            Updating,
            Stopped
        }

        public enum ActorEvents
        {
            Started,
            TwinUpdateFailed,
            TwinUpdated,
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDeviceTwinLogic updateTwinLogic;

        private ActorStatus status;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenToRun;

        /// <summary>
        /// Reference to the actor managing the device state, used
        /// to retrieve the state and prepare the telemetry messages
        /// </summary>
        private IDeviceStateActor deviceStateActor;

        /// <summary>
        /// Reference to the actor managing the device connection
        /// </summary>
        private IDeviceConnectionActor deviceConnectionActor;

        /// <summary>
        /// The telemetry message managed by this actor
        /// </summary>
        public DeviceModel.DeviceModelMessage Message { get; private set; }

        /// <summary>
        /// State maintained by the state actor
        /// </summary>
        public Dictionary<string, object> DeviceState => this.deviceStateActor.DeviceState;

        /// <summary>
        /// Azure IoT Hub client created by the connection actor
        /// </summary>
        public IDeviceClient Client => this.deviceConnectionActor.Client;

        public Services.Models.DeviceTwin DeviceTwin => throw new NotImplementedException();

        public DeviceTwinActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            UpdateTwin updateTwinLogic)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;
            this.updateTwinLogic = updateTwinLogic;

            this.Message = null;

            this.status = ActorStatus.None;
            this.deviceModel = null;
            this.deviceId = null;
            this.deviceStateActor = null;
        }

        public void HandleEvent(DeviceTwinActor.ActorEvents e)
        {
            throw new NotImplementedException();
        }

        public string Run()
        {
            throw new NotImplementedException();
        }

        public void Setup(
            string deviceId,
            Services.Models.DeviceTwin deviceTwin,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
        private void ScheduleTwinUpdate()
        {
            throw new NotImplementedException();
        }

        private void ScheduleTwinUpdateRetry()
        {
            throw new NotImplementedException();
        }

    }
}
