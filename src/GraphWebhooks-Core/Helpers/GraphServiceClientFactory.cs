/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.Graph;
using System.Net.Http.Headers;

namespace GraphWebhooks_Core.Helpers
{
    public static class GraphServiceClientFactory
    {
        public static GraphServiceClient GetAuthenticatedGraphClient(string accessToken)
        {
            return new GraphServiceClient(new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                            Infrastructure.Constants.BearerAuthorizationScheme, accessToken);
                    }));
        }
    }
}
