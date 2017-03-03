/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace GraphWebhooks_Core.Helpers
{
    public class SampleAuthProvider : ISampleAuthProvider
    {
        private readonly IMemoryCache memoryCache;

        // Properties used to get and manage an access token.
        private string aadInstance = Startup.AADInstance;
        private string appId = Startup.AppId;
        private string appSecret = Startup.AppSecret;
        private string graphResourceId = Startup.GraphResourceId;
        private SampleTokenCache tokenCache;
        
        public SampleAuthProvider(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        // Gets an access token. First tries to get the access token from the token cache.
        // This sample uses `tenantId` as part of the cache key because the token is good for all users in the tenant.
        public async Task<string> GetUserAccessTokenAsync(string tenantId)
        {
            tokenCache = new SampleTokenCache(
                tenantId,
                memoryCache);

            AuthenticationContext authContext = new AuthenticationContext($"{ aadInstance }{ tenantId }/v2.0", tokenCache);
            try
            {
                AuthenticationResult authResult = await authContext.AcquireTokenAsync(
                    graphResourceId,
                    new ClientCredential(appId, appSecret)); // For sample purposes only. Production apps should use a client certificate.
                return authResult.AccessToken;
            }
            catch (AdalException e)
            {
                throw e;
            }
        }
    }

    public interface ISampleAuthProvider
    {
        Task<string> GetUserAccessTokenAsync(string tenantId);
    }
}
