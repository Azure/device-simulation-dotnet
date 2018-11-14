using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DeviceList
    {
        public int Total { get; set; }
        public string Prev { get; set; }
        public string Next { get; set; }
        public List<Device> Items { get; set; }

        public DeviceList()
        {
            this.Total = 0;
            this.Prev = string.Empty;
            this.Next = string.Empty;
            this.Items = new List<Device>();
        }
    }
}
