// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    public interface IDevicePropertiesActor
    {
        ISmartDictionary DeviceState { get; }
        IDeviceClient Client { get; }

        void Setup(
            string deviceId,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor);

        string Run();
        void HandleEvent(DevicePropertiesActor.ActorEvents e);
        void Stop();
    }

    public class DevicePropertiesActor : IDevicePropertiesActor
    {
        private enum ActorStatus
        {
            None,
            ReadyToStart,
            ReadyToUpdate,
            Updating,
            Stopped
        }

        public enum ActorEvents
        {
            Started,
            PropertiesUpdateFailed,
            PropertiesUpdated,
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private readonly IRateLimiting rateLimiting;
        private readonly IDevicePropertiesLogic updatePropertiesLogic;

        private ActorStatus status;
        private string deviceId;
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
        /// State maintained by the state actor
        /// </summary>
        public ISmartDictionary DeviceState => this.deviceStateActor.DeviceState;

        /// <summary>
        /// Azure IoT Hub client
        /// </summary>
        public IDeviceClient Client => this.deviceConnectionActor.Client;

        /// <summary>
        /// Azure IoT Hub Device instance
        /// </summary>
        public Device Device => this.deviceConnectionActor.Device;

        public DevicePropertiesActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IRateLimiting rateLimiting,
            IDevicePropertiesLogic updatePropertiesLogic)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.rateLimiting = rateLimiting;
            this.updatePropertiesLogic = updatePropertiesLogic;

            this.status = ActorStatus.None;
            this.deviceId = null;
            this.deviceStateActor = null;
            this.deviceConnectionActor = null;
        }

        /// <summary>
        /// Invoke this method before calling Execute(), to initialize the actor
        /// with details like the device id. Setup() should be called only once.
        /// </summary>
        public void Setup(
            string deviceId,
            IDeviceStateActor deviceStateActor,
            IDeviceConnectionActor deviceConnectionActor)
        {
            if (this.status != ActorStatus.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new { CurrentDeviceId = this.deviceId });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceId = deviceId;
            this.deviceStateActor = deviceStateActor;
            this.deviceConnectionActor = deviceConnectionActor;

            this.updatePropertiesLogic.Setup(this, this.deviceId);
            this.actorLogger.Setup(deviceId, "Properties");

            this.status = ActorStatus.ReadyToStart;
        }

        public void HandleEvent(DevicePropertiesActor.ActorEvents e)
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
        }

        public string Run()
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
            return null;
        }

        public void Stop()
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
        }

        private void ScheduleTwinUpdate()
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
        }

        private void ScheduleTwinUpdateRetry()
        {
            // TODO branch for twin updates to IoT Hub located at:
            //      https://github.com/Azure/device-simulation-dotnet/tree/send-twin-updates
        }

    }
}
