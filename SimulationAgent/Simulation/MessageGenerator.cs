// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Simulation
{
    public interface IMessageGenerator
    {
        DeviceType.DeviceTypeMessage Generate(
            DeviceType deviceType,
            DeviceType.DeviceTypeMessage message);
    }

    public class MessageGenerator : IMessageGenerator
    {
        public DeviceType.DeviceTypeMessage Generate(
            DeviceType deviceType,
            DeviceType.DeviceTypeMessage message)
        {
            string text = message.Message;

            // TODO

            return message;
        }
    }
}
