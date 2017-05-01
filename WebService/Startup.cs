// Copyright (c) Microsoft. All rights reserved.

using System.Web.Http;
using Owin;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            app.UseWebApi(config);
        }
    }
}
