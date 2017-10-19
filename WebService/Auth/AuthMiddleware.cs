// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Auth
{
    /// <summary>
    /// Validate every incoming request checking for a valid authorization header.
    /// The header must containg a valid JWT token. Other than the usual token
    /// validation, the middleware also restrict the allowed algorithms to block
    /// tokens created with a weak algorithm.
    /// Validations used:
    /// * The issuer must match the one in the configuration
    /// * The audience must match the one in the configuration
    /// * The token must not be expired, some configurable clock skew is allowed
    /// * Signature is required
    /// * Signature must be valid
    /// * Signature must be from the issuer
    /// * Signature must use one of the algorithms configured
    /// </summary>
    public class AuthMiddleware
    {
        // The authorization header carries a bearer token, with this prefix
        private const string AUTH_HEADER_PREFIX = "Bearer ";

        // Usual authorization header, carrying the bearer token
        private const string AUTH_HEADER = "Authorization";

        // User requests are marked with this header by the reverse proxy
        // TODO ~devis: this is a temporary solution for public preview only
        // TODO ~devis: remove this approach and use the service to service authentication
        // https://github.com/Azure/pcs-auth-dotnet/issues/18
        // https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet/issues/11
        private const string EXT_RESOURCES_HEADER = "X-Source";

        private const string ERROR401 = @"{""Error"":""Authentication required""}";
        private const string ERROR503_AUTH = @"{""Error"":""Authentication service not available""}";

        private readonly RequestDelegate requestDelegate;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> openIdCfgMan;
        private readonly IClientAuthConfig config;
        private readonly ILogger log;
        private TokenValidationParameters tokenValidationParams;
        private readonly bool authRequired;
        private bool tokenValidationInitialized;

        public AuthMiddleware(
            // ReSharper disable once UnusedParameter.Local
            RequestDelegate requestDelegate, // Required by ASP.NET
            IConfigurationManager<OpenIdConnectConfiguration> openIdCfgMan,
            IClientAuthConfig config,
            ILogger log)
        {
            this.requestDelegate = requestDelegate;
            this.openIdCfgMan = openIdCfgMan;
            this.config = config;
            this.log = log;
            this.authRequired = config.AuthRequired;
            this.tokenValidationInitialized = false;

            // This will show in development mode, or in case auth is turned off
            if (!this.authRequired)
            {
                this.log.Warn("### AUTHENTICATION IS DISABLED! ###", () => { });
                this.log.Warn("### AUTHENTICATION IS DISABLED! ###", () => { });
                this.log.Warn("### AUTHENTICATION IS DISABLED! ###", () => { });
            }
            else
            {
                this.log.Info("Auth config", () => new
                {
                    this.config.AuthType,
                    this.config.JwtIssuer,
                    this.config.JwtAudience,
                    this.config.JwtAllowedAlgos,
                    this.config.JwtClockSkew
                });

                this.InitializeTokenValidationAsync(CancellationToken.None).Wait();
            }

            // TODO ~devis: this is a temporary solution for public preview only
            // TODO ~devis: remove this approach and use the service to service authentication
            // https://github.com/Azure/pcs-auth-dotnet/issues/18
            // https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet/issues/11
            this.log.Warn("### Service to service authentication is not available in public preview ###", () => { });
            this.log.Warn("### Service to service authentication is not available in public preview ###", () => { });
            this.log.Warn("### Service to service authentication is not available in public preview ###", () => { });
        }

        public Task Invoke(HttpContext context)
        {
            var header = string.Empty;
            var token = string.Empty;

            if (!context.Request.Headers.ContainsKey(EXT_RESOURCES_HEADER))
            {
                // This is a service to service request running in the private
                // network, so we skip the auth required for user requests
                // Note: this is a temporary solution for public preview
                // https://github.com/Azure/pcs-auth-dotnet/issues/18
                // https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet/issues/11

                // Call the next delegate/middleware in the pipeline
                this.log.Debug("Skipping auth for service to service request", () => { });
                return this.requestDelegate(context);
            }

            if (!this.authRequired)
            {
                // Call the next delegate/middleware in the pipeline
                this.log.Debug("Skipping auth (auth disabled)", () => { });
                return this.requestDelegate(context);
            }

            if (!this.InitializeTokenValidationAsync(context.RequestAborted).Result)
            {
                context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
                context.Response.Headers["Content-Type"] = "application/json";
                context.Response.WriteAsync(ERROR503_AUTH);
                return Task.CompletedTask;
            }

            if (context.Request.Headers.ContainsKey(AUTH_HEADER))
            {
                header = context.Request.Headers[AUTH_HEADER].SingleOrDefault();
            }
            else
            {
                this.log.Error("Authorization header not found", () => { });
            }

            if (header != null && header.StartsWith(AUTH_HEADER_PREFIX))
            {
                token = header.Substring(AUTH_HEADER_PREFIX.Length).Trim();
            }
            else
            {
                this.log.Error("Authorization header prefix not found", () => { });
            }

            if (this.ValidateToken(token, context) || !this.authRequired)
            {
                // Call the next delegate/middleware in the pipeline
                return this.requestDelegate(context);
            }

            this.log.Warn("Authentication required", () => { });
            context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
            context.Response.Headers["Content-Type"] = "application/json";
            context.Response.WriteAsync(ERROR401);

            return Task.CompletedTask;
        }

        private bool ValidateToken(string token, HttpContext context)
        {
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                SecurityToken validatedToken;
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, this.tokenValidationParams, out validatedToken);
                var jwtToken = new JwtSecurityToken(token);

                // Validate the signature algorithm
                if (this.config.JwtAllowedAlgos.Contains(jwtToken.SignatureAlgorithm))
                {
                    // Store the user info in the request context, so the authorization
                    // header doesn't need to be parse again later in the User controller.
                    context.Request.SetCurrentUserClaims(jwtToken.Claims);

                    return true;
                }

                this.log.Error("JWT token signature algorithm is not allowed.", () => new { jwtToken.SignatureAlgorithm });
            }
            catch (Exception e)
            {
                this.log.Error("Failed to validate JWT token", () => new { e });
            }

            return false;
        }

        private async Task<bool> InitializeTokenValidationAsync(CancellationToken token)
        {
            if (this.tokenValidationInitialized) return true;

            try
            {
                this.log.Info("Initializing OpenID configuration", () => { });
                var openIdConfig = await this.openIdCfgMan.GetConfigurationAsync(token);

                this.tokenValidationParams = new TokenValidationParameters
                {
                    // Validate the token signature
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = openIdConfig.SigningKeys,

                    // Validate the token issuer
                    ValidateIssuer = true,
                    ValidIssuer = this.config.JwtIssuer,

                    // Validate the token audience
                    ValidateAudience = true,
                    ValidAudience = this.config.JwtAudience,

                    // Validate token lifetime
                    ValidateLifetime = true,
                    ClockSkew = this.config.JwtClockSkew
                };

                this.tokenValidationInitialized = true;
            }
            catch (Exception e)
            {
                this.log.Error("Failed to setup OpenId Connect", () => new { e });
            }

            return this.tokenValidationInitialized;
        }
    }
}
