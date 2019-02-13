// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState
{
    public interface IDeviceStateActor
    {
        ISmartDictionary DeviceState { get; }
        ISmartDictionary DeviceProperties { get; }
        bool IsDeviceActive { get; }
        IScriptInterpreter ScriptInterpreter { get; }

        void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            int deviceCounter);

        void Run();
    }

    public class DeviceStateActor : IDeviceStateActor
    {
        private enum ActorStatus
        {
            None,
            Updating
        }

        private const string SUPPORTED_METHODS_KEY = "SupportedMethods";
        private const string TELEMETRY_KEY = "Telemetry";
        private const int START_DISTRIBUTION_WINDOW_MSECS = 10000;

        private readonly UpdateDeviceState updateDeviceStateLogic;
        private readonly ILogger log;
        private readonly IInstance instance;

        private string deviceId;
        private DeviceModel deviceModel;
        private long simulationErrorsCount;
        private long whenCanIUpdate;
        private int startDelayMsecs;
        private ActorStatus status;
        private ISimulationContext simulationContext;

        public ISmartDictionary DeviceState { get; set; }
        public ISmartDictionary DeviceProperties { get; set; }

        public const string CALC_TELEMETRY = "CalculateRandomizedTelemetry";

        /// <summary>
        /// The device is considered active when the state is being updated.
        /// 
        /// By design, rather than talking about "connected devices", we use 
        /// the term "active devices" which is more generic. So when we show
        /// the number of active devices, we can include devices which are not
        /// connected yet but being simulated.
        /// </summary>
        public bool IsDeviceActive => this.status == ActorStatus.Updating;

        public string DeviceId => this.deviceId;

        public IScriptInterpreter ScriptInterpreter { get; }

        public DeviceStateActor(
            UpdateDeviceState updateDeviceStateLogic,
            IScriptInterpreter scriptInterpreter,
            ILogger logger,
            IInstance instance)
        {
            this.updateDeviceStateLogic = updateDeviceStateLogic;
            this.ScriptInterpreter = scriptInterpreter;
            this.log = logger;
            this.instance = instance;
            this.status = ActorStatus.None;
            this.simulationErrorsCount = 0;
        }

        /// <summary>
        /// Simulation error counter in DeviceStateActor
        /// </summary>
        public long SimulationErrorsCount => this.simulationErrorsCount;

        /// <summary>
        /// Invoke this method before calling Start(), to initialize the actor
        /// with details such as the device model and message type to simulate.
        /// If this method is not called before Run(), the application will
        /// throw an exception.
        /// Init() should be called only once, typically after the constructor.
        /// </summary>
        public void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel,
            int deviceCounter)
        {
            this.instance.InitOnce();

            this.simulationContext = simulationContext;
            this.deviceModel = deviceModel;
            this.deviceId = deviceId;

            // Distribute actors start over 10 secs
            this.startDelayMsecs = deviceCounter % START_DISTRIBUTION_WINDOW_MSECS;

            this.instance.InitComplete();
        }

        public void Run()
        {
            this.instance.InitRequired();

            try
            {
                switch (this.status)
                {
                    // Prepare the dependencies
                    case ActorStatus.None:
                        this.updateDeviceStateLogic.Init(this, this.deviceId, this.deviceModel);
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
                this.log.Error("Device state process failed", e);
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
            properties.Set(TELEMETRY_KEY, JToken.FromObject(this.deviceModel.GetTelemetryReportedProperty(this.log)), true);

            // Add SupportedMethods property with methods listed in device model
            properties.Set(SUPPORTED_METHODS_KEY, string.Join(",", this.deviceModel.CloudToDeviceMethods.Keys), true);

            // Add properties listed in device model
            foreach (var property in model.Properties)
            {
                properties.Set(property.Key, JToken.FromObject(property.Value), true);
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
                state.Set("online", true, false);
            }

            // TODO: This is used to control whether telemetry is calculated in UpdateDeviceState.
            //       methods can turn telemetry off/on; e.g. setting temp high- turnoff, set low, turn on
            //       it would be better to do this at the telemetry item level - we should add this in the future
            //       https://github.com/Azure/device-simulation-dotnet/issues/174
            state.Set(CALC_TELEMETRY, true, false);

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
