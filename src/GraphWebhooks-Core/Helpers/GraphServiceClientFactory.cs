/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Graph;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GraphWebhooks_Core.Helpers
{
    public static class GraphServiceClientFactory
    {
        public static async Task<GraphServiceClient> GetAuthenticatedGraphClient(string baseUrl, Func<Task<string>> acquireAccessToken)
        {
            // Fetch the access token
            string accessToken = await acquireAccessToken.Invoke();

            return new GraphServiceClient($"{baseUrl}/beta", new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                            JwtBearerDefaults.AuthenticationScheme, accessToken);
                        return Task.CompletedTask;
                    }));
        }
    }
}
