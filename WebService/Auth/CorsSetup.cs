// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Auth
{
    public interface ICorsSetup
    {
        void UseMiddleware(IApplicationBuilder app);
    }

    public class CorsSetup : ICorsSetup
    {
        private readonly IClientAuthConfig config;
        private readonly ILogger log;

        public CorsSetup(
            IClientAuthConfig config,
            ILogger logger)
        {
            this.config = config;
            this.log = logger;
        }

        public void UseMiddleware(IApplicationBuilder app)
        {
            if (this.config.CorsEnabled)
            {
                this.log.Warn("CORS is enabled", () => { });
                app.UseCors(this.BuildCorsPolicy);
            }
            else
            {
                this.log.Info("CORS is disabled", () => { });
            }
        }

        private void BuildCorsPolicy(CorsPolicyBuilder builder)
        {
            CorsWhitelistModel model;
            try
            {
                model = JsonConvert.DeserializeObject<CorsWhitelistModel>(this.config.CorsWhitelist);
                if (model == null)
                {
                    this.log.Error("Invalid CORS whitelist. Ignored", () => new { this.config.CorsWhitelist });
                    return;
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Invalid CORS whitelist. Ignored", () => new { this.config.CorsWhitelist, ex.Message });
                return;
            }

            if (model.Origins == null)
            {
                this.log.Info("No setting for CORS origin policy was found, ignore", () => { });
            }
            else if (model.Origins.Contains("*"))
            {
                this.log.Info("CORS policy allowed any origin", () => { });
                builder.AllowAnyOrigin();
            }
            else
            {
                this.log.Info("Add specified origins to CORS policy", () => new { model.Origins });
                builder.WithOrigins(model.Origins);
            }

            if (model.Origins == null)
            {
                this.log.Info("No setting for CORS method policy was found, ignore", () => { });
            }
            else if (model.Methods.Contains("*"))
            {
                this.log.Info("CORS policy allowed any method", () => { });
                builder.AllowAnyMethod();
            }
            else
            {
                this.log.Info("Add specified methods to CORS policy", () => new { model.Methods });
                builder.WithMethods(model.Methods);
            }

            if (model.Origins == null)
            {
                this.log.Info("No setting for CORS header policy was found, ignore", () => { });
            }
            else if (model.Headers.Contains("*"))
            {
                this.log.Info("CORS policy allowed any header", () => { });
                builder.AllowAnyHeader();
            }
            else
            {
                this.log.Info("Add specified headers to CORS policy", () => new { model.Headers });
                builder.WithHeaders(model.Headers);
            }
        }
    }
}
