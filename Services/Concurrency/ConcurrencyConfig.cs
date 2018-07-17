// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IConcurrencyConfig
    {
        int TelemetryThreads { get; }
        int MaxPendingConnections { get; }
        int MaxPendingTelemetry { get; }
        int MaxPendingTwinWrites { get; }
        int MinDeviceStateLoopDuration { get; }
        int MinDeviceConnectionLoopDuration { get; }
        int MinDeviceTelemetryLoopDuration { get; }
        int MinDevicePropertiesLoopDuration { get; }
    }

    public class ConcurrencyConfig : IConcurrencyConfig
    {
        private const int DEFAULT_TELEMETRY_THREADS = 4;
        private const int DEFAULT_MAX_PENDING_CONNECTIONS = 200;
        private const int DEFAULT_MAX_PENDING_TELEMETRY = 1000;
        private const int DEFAULT_MAX_PENDING_TWIN_WRITES = 50;
        private const int DEFAULT_MIN_DEVICE_STATE_LOOP_DURATION = 1000;
        private const int DEFAULT_MIN_DEVICE_CONNECTION_LOOP_DURATION = 1000;
        private const int DEFAULT_MIN_DEVICE_TELEMETRY_LOOP_DURATION = 500;
        private const int DEFAULT_MIN_DEVICE_PROPERTIES_LOOP_DURATION = 2000;

        private const int MAX_TELEMETRY_THREADS = 20;
        private const int MAX_MAX_PENDING_CONNECTIONS = 1000;
        private const int MAX_MAX_PENDING_TELEMETRY = 5000;
        private const int MAX_MAX_PENDING_TWIN_WRITES = 500;
        private const int MAX_LOOP_DURATION = 2000;

        private int telemetryThreads;
        private int maxPendingConnections;
        private int maxPendingTelemetry;
        private int maxPendingTwinWrites;
        private int minDeviceStateLoopDuration;
        private int minDeviceConnectionLoopDuration;
        private int minDeviceTelemetryLoopDuration;
        private int minDevicePropertiesLoopDuration;

        public ConcurrencyConfig()
        {
            // Initialize object with default values
            this.TelemetryThreads = DEFAULT_TELEMETRY_THREADS;
            this.MaxPendingConnections = DEFAULT_MAX_PENDING_CONNECTIONS;
            this.MaxPendingTelemetry = DEFAULT_MAX_PENDING_TELEMETRY;
            this.MaxPendingTwinWrites = DEFAULT_MAX_PENDING_TWIN_WRITES;
            this.MinDeviceStateLoopDuration = DEFAULT_MIN_DEVICE_STATE_LOOP_DURATION;
            this.MinDeviceConnectionLoopDuration = DEFAULT_MIN_DEVICE_CONNECTION_LOOP_DURATION;
            this.MinDeviceTelemetryLoopDuration = DEFAULT_MIN_DEVICE_TELEMETRY_LOOP_DURATION;
            this.MinDevicePropertiesLoopDuration = DEFAULT_MIN_DEVICE_PROPERTIES_LOOP_DURATION;
        }

        /// <summary>
        /// How many threads to use to send telemetry.
        /// A value too high (e.g. more than 10) can decrease the overall throughput due to context switching.
        /// A value too low (e.g. less than 2) can decrease the overall throughput due to the time required to
        /// loop through all the devices, when dealing we several thousands of devices.
        /// </summary>
        public int TelemetryThreads
        {
            get => this.telemetryThreads;
            set
            {
                if (value < 1 || value > MAX_TELEMETRY_THREADS)
                {
                    throw new InvalidConfigurationException(
                        "The number of telemetry threads is not valid. " +
                        "Use a value within 1 and " + MAX_TELEMETRY_THREADS);
                }

                this.telemetryThreads = value;
            }
        }

        /// <summary>
        /// While connecting all the devices, limit the number of connections waiting to be
        /// established. A low number will slow down the time required to connect all the devices.
        /// A number too high will increase the number of threads, overloading the CPU.
        /// </summary>
        public int MaxPendingConnections
        {
            get => this.maxPendingConnections;
            set
            {
                if (value < 1 || value > MAX_MAX_PENDING_CONNECTIONS)
                {
                    throw new InvalidConfigurationException(
                        "The max number of pending connections is not valid. " +
                        "Use a value within 1 and " + MAX_MAX_PENDING_CONNECTIONS);
                }

                this.maxPendingConnections = value;
            }
        }

        /// <summary>
        /// While sending telemetry, limit the number of messages waiting to be delivered.
        /// A low number will slow down the simulation.
        /// A number too high will increase the number of threads, overloading the CPU.
        /// </summary>
        public int MaxPendingTelemetry
        {
            get => this.maxPendingTelemetry;
            set
            {
                if (value < 1 || value > MAX_MAX_PENDING_TELEMETRY)
                {
                    throw new InvalidConfigurationException(
                        "The max number of pending telemetry is not valid. " +
                        "Use a value within 1 and " + MAX_MAX_PENDING_TELEMETRY);
                }

                this.maxPendingTelemetry = value;
            }
        }

        /// <summary>
        /// While writing device twins, limit the number of pending operations waiting to be completed.
        /// </summary>
        /// <exception cref="InvalidConfigurationException"></exception>
        public int MaxPendingTwinWrites
        {
            get => this.maxPendingTwinWrites;
            set
            {
                if (value < 1 || value > MAX_MAX_PENDING_TWIN_WRITES)
                {
                    throw new InvalidConfigurationException(
                        "The max number of pending twin writes is not valid. " +
                        "Use a value within 1 and " + MAX_MAX_PENDING_TWIN_WRITES);
                }

                this.maxPendingTwinWrites = value;
            }
        }

        /// <summary>
        /// # When simulating behavior for all the devices in a thread, slow down if the lopp through
        /// all the devices takes less than N msecs. This is also the minimum time between two
        /// state changes for the same device.
        /// </summary>
        public int MinDeviceStateLoopDuration
        {
            get => this.minDeviceStateLoopDuration;
            set
            {
                if (value < 1 || value > MAX_LOOP_DURATION)
                {
                    throw new InvalidConfigurationException(
                        "The min duration of the device state loop is not valid. " +
                        "Use a value within 1 and " + MAX_LOOP_DURATION);
                }

                this.minDeviceStateLoopDuration = value;
            }
        }

        /// <summary>
        /// When connecting the devices, slow down if the lopp through all the devices takes less
        /// than N msecs.
        /// </summary>
        public int MinDeviceConnectionLoopDuration
        {
            get => this.minDeviceConnectionLoopDuration;
            set
            {
                if (value < 1 || value > MAX_LOOP_DURATION)
                {
                    throw new InvalidConfigurationException(
                        "The min duration of the devices connection loop is not valid. " +
                        "Use a value within 1 and " + MAX_LOOP_DURATION);
                }

                this.minDeviceConnectionLoopDuration = value;
            }
        }

        /// <summary>
        /// When sending telemetry for all the devices in a thread, slow down if the lopp through
        /// all the devices takes less than N msecs. This is also the minimum time between two
        /// messages from the same device.
        /// </summary>
        public int MinDeviceTelemetryLoopDuration
        {
            get => this.minDeviceTelemetryLoopDuration;
            set
            {
                if (value < 1 || value > MAX_LOOP_DURATION)
                {
                    throw new InvalidConfigurationException(
                        "The min duration of the device telemetry loop is not valid. " +
                        "Use a value within 1 and " + MAX_LOOP_DURATION);
                }

                this.minDeviceTelemetryLoopDuration = value;
            }
        }

        /// <summary>
        /// When writing device twins for all the devices in a thread, slow down if the lopp through
        /// all the devices takes less than N msecs.
        /// </summary>
        public int MinDevicePropertiesLoopDuration
        {
            get => this.minDevicePropertiesLoopDuration;
            set
            {
                if (value < 1 || value > MAX_LOOP_DURATION)
                {
                    throw new InvalidConfigurationException(
                        "The min duration of the device properties loop is not valid. " +
                        "Use a value within 1 and " + MAX_LOOP_DURATION);
                }

                this.minDevicePropertiesLoopDuration = value;
            }
        }
    }
}
