// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IDeploymentConfig
    {
        string AzureSubscriptionDomain { get; }
        string AzureSubscriptionId { get; }
        string AzureResourceGroup { get; }
        string AzureIothubName { get; }
    }

    public class DeploymentConfig : IDeploymentConfig
    {
        public string AzureSubscriptionDomain { get; set; }
        public string AzureSubscriptionId { get; set; }
        public string AzureResourceGroup { get; set; }
        public string AzureIothubName { get; set; }
    }
}
