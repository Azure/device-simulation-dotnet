using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceReplay
{
    public interface IDeviceReplayActor
    {
        void Init(
            ISimulationContext simulationContext,
            string deviceId,
            DeviceModel deviceModel);

        bool HasWorkToDo();
        Task RunAsync();
        void Stop();
    }

    public class DeviceReplayActor : IDeviceReplayActor
    {
        private enum ActorStatus
        {
            None,
            Stopped
        }

        private readonly ILogger log;
        private readonly IActorsLogger actorLogger;
        private ISimulationContext simulationContext;
        private DeviceModel deviceModel;
        private readonly IInstance instance;

        private ActorStatus status;
        private string deviceId;

        public DeviceReplayActor(
            ILogger logger,
            IActorsLogger actorLogger,
            IInstance instance)
        {
            this.log = logger;
            this.actorLogger = actorLogger;
            this.instance = instance;

            this.status = ActorStatus.None;
            this.deviceModel = null;
        }

        /// <summary>
        /// TODO: Fill in comment
        /// </summary>
        public void Init(
            ISimulationContext simulationContext, 
            string deviceId, 
            DeviceModel deviceModel)
        {
            this.instance.InitOnce();

            this.simulationContext = simulationContext;
            this.deviceModel = deviceModel;
            this.deviceId = deviceId;
            this.actorLogger.Init(deviceId, "Replay");

            this.instance.InitComplete();
        }

        public bool HasWorkToDo()
        {
            // TODO: Compute work to do
            return false;
        }

        public async Task RunAsync()
        {
            // TODO: Perform asyn action
        }

        public void Stop()
        {
            this.log.Debug("Device replay actor stopped",
                () => new { this.deviceId, Status = this.status.ToString() });

            this.status = ActorStatus.Stopped;
        }
    }
}
