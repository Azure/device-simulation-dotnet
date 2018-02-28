// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState
{
    public interface IDeviceStateActor
    {
        IInternalDeviceState DeviceState { get; }
        bool IsDeviceActive { get; }
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

        /// <summary>
        /// The virtual state of the simulated device. The state is composed of 
        /// both the device reported properties (device values like firmware etc..)
        /// and the device simulation state (simulated sensor data like temperature etc...)
        /// that are periodically updated using an external script.
        /// </summary>
        public IInternalDeviceState DeviceState { get; set; }

        public const string CALC_TELEMETRY = "CalculateRandomizedTelemetry";

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
            get
            {
                return this.status == ActorStatus.Updating;
            }
        }

        private readonly ILogger log;
        private readonly UpdateDeviceState updateDeviceStateLogic;
        private string deviceId;
        private DeviceModel deviceModel;
        private long whenCanIUpdate;
        private int startDelayMsecs;
        private ActorStatus status;

        public DeviceStateActor(
            ILogger logger,
            UpdateDeviceState updateDeviceStateLogic)
        {
            this.log = logger;
            this.updateDeviceStateLogic = updateDeviceStateLogic;
            this.status = ActorStatus.None;
        }

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
                        this.DeviceState = new InternalDeviceState(this.deviceModel, this.log);
                        this.log.Debug("Initial device state", () => new { this.deviceId, this.DeviceState });
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
    }
}