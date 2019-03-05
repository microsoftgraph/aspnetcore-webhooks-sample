using GraphWebhooks_Core.Infrastructure;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GraphWebhooks_Core.Helpers
{
    public class SampleAuthProvider : ISampleAuthProvider
    {
        private readonly IMemoryCache memoryCache;
        private readonly AppSettings appSettings;
        private readonly AzureADOptions azureAdOptions;

        // Properties used to get and manage an access token.
        private string aadInstance { get; set; }
        private string appId { get; set; }
        private string appSecret { get; set; }
        private string graphResourceId { get; set; }
        private SampleTokenCache tokenCache;

        // Dependency injection constructor.
        public SampleAuthProvider(IMemoryCache memoryCache,
                                  IOptions<AppSettings> appSettingsAccessor,
                                  IConfiguration configuration)
        {
            this.memoryCache = memoryCache;
            appSettings = appSettingsAccessor.Value;
            azureAdOptions = new AzureADOptions();
            configuration.Bind("AzureAd", azureAdOptions);

            aadInstance = azureAdOptions.Instance;
            appId = azureAdOptions.ClientId;
            appSecret = azureAdOptions.ClientSecret;
            graphResourceId = appSettings.GraphApiUrl;
        }

        // Gets an access token. First tries to get the access token from the token cache.
        // This sample uses a password (secret) to authenticate. Production apps should use a certificate.

        //public async Task<string> GetUserTokenAsync(string tenantId)
        //{

        //}

        public async Task<string> GetUserAccessTokenAsync(string tenantId)
        {
            tokenCache = new SampleTokenCache(
                tenantId,
                memoryCache);

            AuthenticationContext authContext = new AuthenticationContext($"{ aadInstance }common", tokenCache);
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
