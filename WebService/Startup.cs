﻿// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Web.Http.Routing;
using Owin;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    /// <summary>Application wrapper started by the entry point, see Program.cs</summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            SetupDependencyInjection(app, config);

            config.AddApiVersioning(o =>
            {
                // When this property is set to `true`, the HTTP headers
                // "api-supported-versions" and "api-deprecated-versions" will
                // be added to all valid service routes. This information is
                // useful for advertising which versions are supported and
                // scheduled for deprecation to clients. This information is
                // also useful when supporting the OPTIONS verb.
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = false;
            });

            config.Routes.MapHttpRoute(
                name: "VersionedApi",
                routeTemplate: "v{apiVersion}/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional },
                constraints: new { apiVersion = new ApiVersionRouteConstraint() });

            app.UseWebApi(config);
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// http://docs.autofac.org/en/latest/integration/owin.html
        /// http://autofac.readthedocs.io/en/latest/register/scanning.html
        /// </summary>
        private static void SetupDependencyInjection(IAppBuilder app, HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            // Register Web API controller in executing assembly.
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // Autowire interfaces to classes. Note that the solution assemblies
            // are explicitly managed here. This could be extended to analyze
            // all the assemblies directly and indirectly referenced.
            var assembly = Assembly.GetExecutingAssembly();
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            // Custom rules overriding autowired ones.
            SetupCustomDependencyInjection(builder);

            // Create and assign a dependency resolver for Web API to use.
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            // The Autofac middleware should be the first middleware added to
            // the IAppBuilder.
            app.UseAutofacMiddleware(container);

            // Make sure the Autofac lifetime scope is passed to Web API.
            app.UseAutofacWebApi(config);
        }

        private static void SetupCustomDependencyInjection(ContainerBuilder builder)
        {
            // Make sure the configuration is read only once
            var config = new Config();
            builder.RegisterInstance(config).As<IConfig>();
        }
    }
}
