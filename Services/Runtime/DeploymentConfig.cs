// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IDeploymentConfig
    {
        string AzureSubscriptionDomain { get; }
        string AzureSubscriptionId { get; }
        string AzureResourceGroup { get; }
        string AzureResourceGroupLocation { get; }
        string AzureIothubName { get; }
        string AzureVmssName { get; }
        string AadTenantId { get; }
        string AadAppId { get; }
        string AadAppSecret { get; }
        string AadTokenUrl { get; }
    }

    public class DeploymentConfig : IDeploymentConfig
    {
        public string AzureSubscriptionDomain { get; set; }
        public string AzureSubscriptionId { get; set; }
        public string AzureResourceGroup { get; set; }
        public string AzureResourceGroupLocation { get; set; }
        public string AzureIothubName { get; set; }
        public string AzureVmssName { get; set; }
        public string AadTenantId { get; set; }
        public string AadAppId { get; set; }
        public string AadAppSecret { get; set; }
        public string AadTokenUrl { get; set; }
    }
}
