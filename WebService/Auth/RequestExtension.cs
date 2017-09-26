// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Auth
{
    public static class RequestExtension
    {
        private const string CONTEXT_KEY = "CurrentUserClaims";

        // Store the current user claims in the current request
        public static void SetCurrentUserClaims(this HttpRequest request, IEnumerable<Claim> claims)
        {
            request.HttpContext.Items[CONTEXT_KEY] = claims;
        }

        // Get the user claims from the current request
        public static IEnumerable<Claim> GetCurrentUserClaims(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(CONTEXT_KEY))
            {
                return new List<Claim>();
            }

            return request.HttpContext.Items[CONTEXT_KEY] as IEnumerable<Claim>;
        }
    }
}
