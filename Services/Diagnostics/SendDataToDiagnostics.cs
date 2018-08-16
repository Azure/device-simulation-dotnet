using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public class SendDataToDiagnostics
    {
        private readonly IHttpClient httpClient;
        
        public SendDataToDiagnostics(ILogger logger)
        {
            this.httpClient = new HttpClient(logger);
        }

        public async void SendToDiagnostics(object jobj)
        {
            var response1 = await httpClient.PostAsync(this.PrepareRequest("http://localhost:9006/v1/diagnosticsevents", jobj));
        }

        public void CreateErrorObject(string message)
        {
            dynamic jobj = new JObject();
            jobj.DeploymentId = "undefined";
            jobj.EventType = "Error";
            jobj.Timestamp = DateTime.Now;
            this.SendToDiagnostics(jobj);
        }

        public void SendSimulationDetails(string eventType)
        {
            dynamic jobj = new JObject();
            jobj.DeploymentId = "undefined";
            if (eventType.Equals("Heartbeat"))
            {
                jobj.EventType = "Simulation Service Heartbeat";
            }
            else if (eventType.Equals("Service_start"))
            {
                jobj.EventType = "Simulation Service Starting";
            }
            jobj.Timestamp = DateTime.Now;
            this.SendToDiagnostics(jobj);
        }
        
        private HttpRequest PrepareRequest(string path, object obj=null)
        {
            var request = new HttpRequest();
            request.AddHeader(HttpRequestHeader.Accept.ToString(), "application/json");
            request.SetUriFromString(path);
            request.SetContent(obj);
            return request;
        }

    }
}
