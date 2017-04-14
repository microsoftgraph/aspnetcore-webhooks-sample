/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Extensions.Options;

namespace GraphWebhooks_Core.Helpers
{
    public class SampleAuthProvider : ISampleAuthProvider
    {
        private readonly IMemoryCache memoryCache;
        private readonly AppSettings appSettings;
        
        // Properties used to get and manage an access token.
        private string aadInstance { get; set; }
        private string appId { get; set; }
        private string appSecret { get; set; }
        private string graphResourceId { get; set; }
        private SampleTokenCache tokenCache;
        
        // Dependency injection constructor.
        public SampleAuthProvider(IMemoryCache memoryCache,
                                  IOptions<AppSettings> optionsAccessor)
        {
            this.memoryCache = memoryCache;
            appSettings = optionsAccessor.Value;

            aadInstance = appSettings.AADInstance;
            appId = appSettings.AppId;
            appSecret = appSettings.AppSecret;
            graphResourceId = appSettings.GraphResourceId;
        }

        // Gets an access token. First tries to get the access token from the token cache.
        // This sample uses a password (secret) to authenticate. Production apps should use a certificate.
        public async Task<string> GetUserAccessTokenAsync(string tenantId)
        {
            tokenCache = new SampleTokenCache(
                tenantId,
                memoryCache);

            AuthenticationContext authContext = new AuthenticationContext($"{ aadInstance }{ tenantId }", tokenCache);
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
