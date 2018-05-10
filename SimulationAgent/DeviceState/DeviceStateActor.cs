// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState
{
    public interface IDeviceStateActor
    {
        ISmartDictionary DeviceState { get; }
        ISmartDictionary DeviceProperties { get; }
        bool IsDeviceActive { get; }
        long SimulationErrorsCount { get; }
        void Setup(string deviceId, DeviceModel deviceModel, int position, int totalDevices);
        void Run();
    }

    public class DeviceStateActor : IDeviceStateActor
    {
        public enum ActorStatus
        {
            None,
            Updating
        }

        public ISmartDictionary DeviceState { get; set; }
        public ISmartDictionary DeviceProperties { get; set; }

        public const string CALC_TELEMETRY = "CalculateRandomizedTelemetry";
        public const string SUPPORTED_METHODS_KEY = "SupportedMethods";
        public const string TELEMETRY_KEY = "Telemetry";

        private readonly ILogger log;
        private readonly UpdateDeviceState updateDeviceStateLogic;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenCanIUpdate;
        private int startDelayMsecs;
        private ActorStatus status;
        private long simulationErrorsCount;

        /// <summary>
        /// The device is considered active when the state is being updated.
        /// 
        /// By design, rather than talking about "connected devices", we use 
        /// the term "active devices" which is more generic. So when we show
        /// the number of active devices, we can include devices which are not
        /// connected yet but being simulated.
        /// </summary>
        public bool IsDeviceActive
        {
            get { return this.status == ActorStatus.Updating; }
        }

        public DeviceStateActor(
            ILogger logger,
            UpdateDeviceState updateDeviceStateLogic)
        {
            this.log = logger;
            this.updateDeviceStateLogic = updateDeviceStateLogic;
            this.status = ActorStatus.None;
            this.simulationErrorsCount = 0;
        }

        /// <summary>
        /// Simulation error counter in DeviceStateActor
        /// </summary>
        public long SimulationErrorsCount => this.simulationErrorsCount;

        /// <summary>
        /// Invoke this method before calling Start(), to initialize the actor
        /// with details like the device model and message type to simulate.
        /// If this method is not called before Start(), the application will
        /// throw an exception.
        /// Setup() should be called only once, typically after the constructor.
        /// </summary>
        public void Setup(string deviceId, DeviceModel deviceModel, int position, int totalDevices)
        {
            if (this.status != ActorStatus.None)
            {
                this.log.Error("The actor is already initialized",
                    () => new { CurrentDeviceId = this.deviceId, NewDeviceModelId = deviceModel.Id });
                throw new DeviceActorAlreadyInitializedException();
            }

            this.deviceModel = deviceModel;
            this.deviceId = deviceId;

            // Distributed start times over 1 or 10 secs
            var msecs = totalDevices < 50 ? 1000 : 10000;
            this.startDelayMsecs = (int) (msecs * ((double) position / totalDevices));
        }

        public void Run()
        {
            try
            {
                switch (this.status)
                {
                    // Prepare the dependencies
                    case ActorStatus.None:
                        this.updateDeviceStateLogic.Setup(this, this.deviceId, this.deviceModel);
                        this.DeviceState = this.GetInitialState(this.deviceModel);
                        this.DeviceProperties = this.GetInitialProperties(this.deviceModel);
                        this.log.Debug("Initial device state", () => new { this.deviceId, this.DeviceState, this.DeviceProperties });
                        this.MoveForward();
                        return;

                    // Update the device state
                    case ActorStatus.Updating:
                        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        if (now >= this.whenCanIUpdate)
                        {
                            this.log.Debug("Time to update the device state", () => new { this.deviceId });
                            this.updateDeviceStateLogic.Run();
                            this.MoveForward();
                        }

                        return;
                }

                throw new Exception("Application error, Execute() should not be invoked when status = " + this.status);
            }
            catch (Exception e)
            {
                this.simulationErrorsCount++;
                this.log.Error("Device state process failed", () => new { e });
            }
        }

        private void MoveForward()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            switch (this.status)
            {
                case ActorStatus.None:
                    this.whenCanIUpdate = now + this.startDelayMsecs;
                    this.log.Debug("Next update scheduled",
                        () => new { this.deviceId, when = DateTimeOffset.FromUnixTimeMilliseconds(this.whenCanIUpdate).ToString("u") });
                    this.status = ActorStatus.Updating;
                    return;

                case ActorStatus.Updating:
                    this.whenCanIUpdate += (long) this.deviceModel.Simulation.Interval.TotalMilliseconds;
                    return;
            }

            throw new Exception("Application error, MoveForward() should not be invoked when status = " + this.status);
        }

        /// <summary>
        /// Initializes device properties from the device model.
        /// </summary>
        private ISmartDictionary GetInitialProperties(DeviceModel model)
        {
            var properties = new SmartDictionary();
            
            if (model.Properties == null || this.deviceModel.CloudToDeviceMethods == null) return properties;

            // Add telemetry property
            properties.Set(TELEMETRY_KEY, JToken.FromObject(this.deviceModel.GetTelemetryReportedProperty(this.log)));

            // Add SupportedMethods property with methods listed in device model
            properties.Set(SUPPORTED_METHODS_KEY, string.Join(",", this.deviceModel.CloudToDeviceMethods.Keys));

            // Add properties listed in device model
            foreach (var property in model.Properties)
            {
                properties.Set(property.Key, JToken.FromObject(property.Value));
            }

            return properties;
        }

        /// <summary>
        /// Initializes device state from the device model.
        /// </summary>
        private ISmartDictionary GetInitialState(DeviceModel model)
        {
            var initialState = CloneObject(model.Simulation.InitialState);

            var state = new SmartDictionary(initialState);

            // Ensure the state contains the "online" key
            if (!state.Has("online"))
            {
                state.Set("online", true);
            }

            // TODO: This is used to control whether telemetry is calculated in UpdateDeviceState.
            //       methods can turn telemetry off/on; e.g. setting temp high- turnoff, set low, turn on
            //       it would be better to do this at the telemetry item level - we should add this in the future
            //       https://github.com/Azure/device-simulation-dotnet/issues/174
            state.Set(CALC_TELEMETRY, true);

            return state;
        }

        /// <summary>Copy an object by value</summary>
        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(source));
        }
    }
}
